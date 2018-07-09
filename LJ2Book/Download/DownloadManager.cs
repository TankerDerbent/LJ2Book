using CefSharp;
using LJ2Book.DataBase;
using LJ2Book.LiveJournalAPI;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using static LJ2Book.LiveJournalAPI.Connection;

namespace LJ2Book.Download
{
	class DownloadManager
	{
		public delegate void OnArticlesLoadProgressChanged(int MaxItems);
		public event OnArticlesLoadProgressChanged ArticlesLoadProgressChanged;
		public delegate void OnArticlesLoadProgressStep();
		public event OnArticlesLoadProgressStep ArticlesLoadProgressStep;
		private static Connection cnn;
		private static object cnnSyncObject = new object();
		private SynchronizationContext WpfSyncContext;
		private Semaphore semaphore;
		private const int DOWNLOAD_THREADS = 3;
		public DownloadManager(SynchronizationContext _ctx)
		{
			WpfSyncContext = _ctx;
			semaphore = new Semaphore(0, DOWNLOAD_THREADS);
		}
		public static bool TryLogin(string _Login, string _encryptedPass)
		{
			lock (cnnSyncObject)
			{
				cnn = new Connection(_Login, _encryptedPass);
				return cnn.CheckConnection();
			}
		}
		public void Update(Blog _blog)
		{
			if (cnn == null || !cnn.CheckConnection())
				throw new InvalidOperationException();

			int LastEventNo = -1;
			lock (cnnSyncObject)
				LastEventNo = cnn.GetLastEventNo(_blog.User.UserName);

			Blog blog;
			lock (App.dbLock)
			{
				blog = App.db.Blogs.Find(_blog.UserBlogID);
				bool NeedUpdate = LastEventNo > blog.LastItemNo;
				blog.LastItemNo = LastEventNo;
				blog.LastSync = DateTime.Now;
				App.db.Entry(blog).State = EntityState.Modified;
				App.db.SaveChanges();
			}
			if (BlogInfoArrived != null)
				BlogInfoArrived(blog.LastItemNo, blog.LastSync);

			if (blog.KindOfSynchronization == KindOfSynchronization.Auto)
				SyncBlog(blog);
		}
		public delegate void OnBlogInfoArrived(int lastItemNo, DateTime dateLastSync);
		public event OnBlogInfoArrived BlogInfoArrived;

		private void SyncBlog(Blog _blog)
		{
			List<int> listItemsToSync;
			lock (App.dbLock)
			{
				App.db.Entry(_blog).Collection(b => b.Articles).Load();
				if (_blog.Articles == null)
				{
					listItemsToSync = Enumerable.Range(1, _blog.LastItemNo).ToList();
				}
				else
				{
					var qryArticles = from a in _blog.Articles select a.ArticleNo;
					
					listItemsToSync = Enumerable.Range(1, _blog.LastItemNo).Except(qryArticles.ToArray()).ToList();
				}
			}
			int NumberItemsToLoad = listItemsToSync.Count;
			if (NumberItemsToLoad == 0)
			{
				Debug.WriteLine("Articles to load: nothing");
				return;
			}
			Debug.WriteLine("Articles to load: " + string.Join(",", (from i in listItemsToSync select i.ToString()).ToArray()));

			if (ArticlesLoadProgressChanged != null)
				ArticlesLoadProgressChanged(NumberItemsToLoad);

			DownloadManagerTaskInfo[] DownloadInfos = new DownloadManagerTaskInfo[NumberItemsToLoad];
			for (int i = 0; i < NumberItemsToLoad; i++)
				DownloadInfos[i] = new DownloadManagerTaskInfo { Target = _blog.User.UserName, ItemNo = listItemsToSync[i], sc = this.WpfSyncContext, blog = _blog };

			for (int i = 0; i < NumberItemsToLoad; i++)
				ThreadPool.QueueUserWorkItem(DownloadThread, DownloadInfos[i]);

			semaphore.Release(DOWNLOAD_THREADS);
		}

		private class DownloadManagerTaskInfo
		{
			public string Target;
			public int ItemNo;
			public SynchronizationContext sc;
			public Blog blog;
		}
		private void DownloadThread(Object _di)
		{
			if (!(_di is DownloadManagerTaskInfo))
				return;

			semaphore.WaitOne();

			DownloadManagerTaskInfo di = _di as DownloadManagerTaskInfo;

			//Debug.WriteLine("Task {0}: started", di.ItemNo);

			LiveJournalEvent ev;
			try
			{
				lock (cnnSyncObject)
				{
					ev = cnn.GetEventByNo(di.Target, di.ItemNo);
					//Debug.WriteLine("DWM: Task {0}: for target {1} got event {2}, tags '{3}'", di.ItemNo, di.Target, ev.url, ev.Params.ContainsKey("taglist") ? ev.Params["taglist"] : string.Empty);
				}
			}
			catch (FailedToGetEventByNoException e)
			{
				Debug.WriteLine(e.Message);
				semaphore.Release();
				if (ArticlesLoadProgressStep != null)
					ArticlesLoadProgressStep();
				return;
			}
			Article article = new Article
			{
				ArticleNo = di.ItemNo,
				Anum = ev.anum,
				State = ArticleState.Queued,
				Url = ev.url,
				ArticleDT = ev.eventtime,
				RawTitle = string.Empty,
				RawBody = string.Empty,
				Tags = ev.Params.ContainsKey("taglist") ? ev.Params["taglist"].Replace(", ", ",") : string.Empty,
				Blog = di.blog
			};
			ManualResetEvent evt = new ManualResetEvent(false);
			di.sc.Post(new SendOrPostCallback((o)=>
			{
				lock (App.dbLock)
				{
					try
					{
						App.db.Articles.Add(article);
						App.db.SaveChanges();
						//Debug.WriteLine("Task {0}: article No {1} for '{2}' saved", di.ItemNo, article.AtricleNo, article.Blog.User.UserName);
						evt.Set();
					}
					catch (System.Data.Entity.Infrastructure.DbUpdateException e)
					{
						if (e.InnerException is System.Data.Entity.Core.UpdateException)
						{
							System.Data.Entity.Core.UpdateException e2 = (e.InnerException as System.Data.Entity.Core.UpdateException);
							if (e2.InnerException is System.Data.SQLite.SQLiteException)
							{
								System.Data.SQLite.SQLiteException e3 = e2.InnerException as System.Data.SQLite.SQLiteException;
								if (e3.ResultCode == System.Data.SQLite.SQLiteErrorCode.Constraint)
								{
									MessageBox.Show(string.Format("Fail to insert article No: '{0}'", article.ArticleNo.ToString()));
								}
							}
						}
						else
							throw e;
					}
				}
			}), null);
			HtmlLoader loaderStage2 = new HtmlLoader(article, di.sc, evt, this);
		}
		public void SaveArticleDetails(Article article, List<Picture> pictures)
		{
			semaphore.Release();
			lock (App.dbLock)
			{
				var context = App.db;
				context.Entry(article).State = EntityState.Modified;
				context.SaveChanges();
				foreach (var p in pictures)
					context.Pictures.Add(p);
				context.SaveChanges();
				if (ArticlesLoadProgressStep != null)
					ArticlesLoadProgressStep();
				//Debug.WriteLine("DWM: Task '{0}' url '{1}',  details saved to DB, semaphore released", article.ArticleNo, article.Url);
			}
		}
	}

	class HtmlLoader
	{
		Article Article;
		CefSharp.OffScreen.ChromiumWebBrowser browser;
		SynchronizationContext syncContext;
		ManualResetEvent evt;
		DownloadManager downloadManager;
		public HtmlLoader(Article _article, SynchronizationContext _syncContext, ManualResetEvent manualResetEvent, DownloadManager _downloadManager)
		{
			this.Article = _article;
			syncContext = _syncContext;
			evt = manualResetEvent;
			downloadManager = _downloadManager;
			if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
			{
				//Debug.WriteLine("HtmlLoader for 919: ctor");
			}

			browser = new CefSharp.OffScreen.ChromiumWebBrowser();
			browser.BrowserInitialized += Browser_BrowserInitialized;
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
		}
		private string[] Images;

		private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
		{
			if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
			{
				//Debug.WriteLine("HtmlLoader for 919: Browser_FrameLoadEnd got url '{0}'", e.Url);
			}
			//       "https://www.livejournal.com/login.bml?returnto=https:%2F%2Ftestdev666.livejournal.com%2F919.html"
			if (e.Url.StartsWith("https://www.livejournal.com/login.bml"))
				SavePageToDB(false);
			if (e.Url != Article.Url)
				return;
			ExtractArticleTitle();
		}
		private async void ExtractArticleTitle()
		{
			if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
			{
				//Debug.WriteLine("HtmlLoader for 919: ExtractArticleTitle start");
			}

			var task = browser.EvaluateScriptAsync("(function() {{ var x = document.getElementsByClassName('aentry-post__title-text'); return x.length > 0 ? x[0].innerHTML : '';}} )();");
			await task.ContinueWith(t =>
			{
				if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
				{
					//Debug.WriteLine("HtmlLoader for 919: ExtractArticleTitle task");
				}

				if (!t.IsFaulted)
				{
					var response = t.Result;
					Article.RawTitle = response.Success ? response.Result.ToString() : string.Empty;
					//Debug.WriteLine("Thread {0}: Tittle loaded: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
					ExtractArticleBody();
				}
				else
					SavePageToDB(false);
			});
		}
		private async void ExtractArticleBody()
		{
			if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
			{
				//Debug.WriteLine("HtmlLoader for 919: ExtractArticleBody start");
			}

			string script = "(function() {{ var x = document.getElementsByClassName('aentry-post__text'); return x.length > 0 ? x[0].innerHTML : '';}} )();";
			if (Article.RawTitle.Length < 1)
				script = script.Replace("aentry-post__text", "b-singlepost-body");

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
				{
					//Debug.WriteLine("HtmlLoader for 919: ExtractArticleBody task");
				}

				if (!t.IsFaulted)
				{
					var response = t.Result;
					Article.RawBody = response.Success ? response.Result.ToString() : string.Empty;
					//Debug.WriteLine("Thread {0}: Body loaded: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
					ExtractImageList();
				}
				else
					SavePageToDB(false);
			});
		}
		private async void ExtractImageList()
		{
			if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
			{
				//Debug.WriteLine("HtmlLoader for 919: ExtractImageList start");
			}

			string script = "(function() {{ var imgs = document.getElementsByClassName('aentry-post__text')[0].getElementsByTagName('img'); var sImgs = '&'; for (var i = 0; i < imgs.length; i++) { sImgs += (imgs[i].src + '&');} return sImgs;}} )();";
			if (Article.RawTitle.Length < 1)
				script = script.Replace("aentry-post__text", "b-singlepost-body");

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
				{
					//Debug.WriteLine("HtmlLoader for 919: ExtractImageList task");
				}

				if (!t.IsFaulted)
				{
					var response = t.Result;
					var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
					if (response.Success)
					{
						List<string> result = new List<string>();
						foreach (var s in EvaluateJavaScriptResult.ToString().Split('&'))
							if (s.Length > 0)
								result.Add(s);
						Images = result.ToArray();
						string resultStr = string.Join("\r\n", EvaluateJavaScriptResult.ToString().Split('&'));
						//Debug.WriteLine(string.Format("Got images list: \r\n{0}", resultStr));
						//Debug.WriteLine("Thread {0}: Image list loaded: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
					}
					else
					{
						//Debug.WriteLine("Thread {0}: Got EMPTY images list\r\n", Thread.CurrentThread.ManagedThreadId);
					}
					DownloadPictures();
				}
				else
					SavePageToDB(false);
			});
		}
		private List<Picture> Pictures = new List<Picture>();
		private void DownloadPictures()
		{
			if (Images.Count() > 0)
			{
				App.db.Pictures.Load();
				string[] ImagesToLoad = Images.Except((from p in App.db.Pictures select p.Url).ToArray()).ToArray();
				foreach (var url in ImagesToLoad)
				{
					byte[] blob;
					if (DownloadRemoteImageFile(url, out blob))
						Pictures.Add(new Picture { Url = url, Data = blob });
				}
			}
			SavePageToDB();
		}

		private static bool DownloadRemoteImageFile(string uri, out byte[] blob)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch(System.Net.WebException e)
			{
				Debug.WriteLine("DownloadRemoteImageFile: got exception: " + e.Message);
				blob = new byte[] { 1 };
				return false;
			}

			// Check that the remote file was found. The ContentType
			// check is performed since a request for a non-existent
			// image file might be redirected to a 404-page, which would
			// yield the StatusCode "OK", even though the image was not
			// found.
			if ((response.StatusCode == HttpStatusCode.OK ||
				response.StatusCode == HttpStatusCode.Moved ||
				response.StatusCode == HttpStatusCode.Redirect) &&
				response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
			{

				// if the remote file was found, download oit
				using (Stream inputStream = response.GetResponseStream())
				using (MemoryStream outputStream = new MemoryStream())
				{
					byte[] buffer = new byte[4096];
					int bytesRead;
					do
					{
						bytesRead = inputStream.Read(buffer, 0, buffer.Length);
						outputStream.Write(buffer, 0, bytesRead);
					} while (bytesRead != 0);
					blob = outputStream.ToArray();
					return true;
				}
			}
			blob = new byte[] { 1 };
			return false;
		}


		private void SavePageToDB(bool Success = true)
		{
			browser.BrowserInitialized -= Browser_BrowserInitialized;
			browser.FrameLoadEnd -= Browser_FrameLoadEnd;

			evt.WaitOne();

			if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
			{
				//Debug.WriteLine("HtmlLoader for 919: SavePageToDB start");
			}


			Debug.WriteLine("Thread {0}: Event setd: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
			syncContext.Post(new SendOrPostCallback((o) =>
			{
				if (this.Article.Url == "https://testdev666.livejournal.com/919.html")
				{
					//Debug.WriteLine("HtmlLoader for 919: SavePageToDB ctx.Post");
				}

				Article.State = Success ? ArticleState.Ready : ArticleState.FailedToProcess;
				downloadManager.SaveArticleDetails(Article, Pictures);
			}), null);
		}

		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("Thread {0}: start load '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
			browser.Load(Article.Url);
			//browser.LoadHtml(rawText, Article.Url);
		}
	}
}
