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
		public event OnArticlesLoadProgressStep ArticlesLoadProgressStepStage1;
		public event OnArticlesLoadProgressStep ArticlesLoadProgressStepStage2;
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
			string logString = string.Format("Articles to load: " + string.Join(",", (from i in listItemsToSync select i.ToString()).ToArray()));
			if (logString.Length > 150)
				Debug.WriteLine(logString.Substring(0, 130) + "...<skipped>..." + logString.Substring(logString.Length - 15, 15));
			else
				Debug.WriteLine(logString);

			if (ArticlesLoadProgressChanged != null)
				ArticlesLoadProgressChanged(NumberItemsToLoad);

			List<DownloadManagerTaskInfo> diList = new List<DownloadManagerTaskInfo>();
			foreach (var i in listItemsToSync)
			{
				Thread.Sleep(205);
				try
				{
					LiveJournalEvent Event = cnn.GetEventByNo(_blog.User.UserName, i);
					if (Event == null)
					{
						Debug.WriteLine(string.Format("Get Event by No: event for {0} is deleted", i));
					}
					else
					{
						Debug.WriteLine(string.Format("Get Event by No: event for {0} has URL {1}", i, Event.url));
					}
				}
				catch (FailedToGetEventByNoException)
				{
					Debug.WriteLine(string.Format("Get Event by No: Fail to get event for {0}.", i));
				}
			}
			return;

			//DownloadManagerTaskInfo[] DownloadInfos = new DownloadManagerTaskInfo[NumberItemsToLoad];
			//for (int i = 0; i < NumberItemsToLoad; i++)
			//	DownloadInfos[i] = new DownloadManagerTaskInfo { Target = _blog.User.UserName, ItemNo = listItemsToSync[i], sc = this.WpfSyncContext, blog = _blog, ShouldProcessPage = false };

			//for (int i = 0; i < NumberItemsToLoad; i++)
			//	ThreadPool.QueueUserWorkItem(DownloadThread, DownloadInfos[i]);

			//semaphore.Release(DOWNLOAD_THREADS);
		}

		private class DownloadManagerTaskInfo
		{
			public string Target;
			public int ItemNo;
			LiveJournalEvent Event;
			public SynchronizationContext sc;
			public Blog blog;
			public bool ShouldProcessPage;
		}
		private void DownloadThread(Object _di)
		{
			if (!(_di is DownloadManagerTaskInfo))
				return;

			semaphore.WaitOne();

			DownloadManagerTaskInfo di = _di as DownloadManagerTaskInfo;
			Debug.WriteLine("Task started for item No {0}: lock semaphore", di.ItemNo);

			LiveJournalEvent ev;
			try
			{
				lock (cnnSyncObject)
				{
					ev = cnn.GetEventByNo(di.Target, di.ItemNo);
				}
			}
			catch (FailedToGetEventByNoException)
			{
				Debug.WriteLine("Task for item No {0}: release semaphore (fail to get event)", di.ItemNo);
				semaphore.Release();
				if (ArticlesLoadProgressStepStage2 != null)
					ArticlesLoadProgressStepStage2();
				return;
			}
			if (ev == null)
			{
				Debug.WriteLine("Thread {0}: Article {1} has been deleted", Thread.CurrentThread.ManagedThreadId, di.ItemNo);
				Article article = new Article
				{
					ArticleNo = di.ItemNo,
					Anum = 0,
					State = ArticleState.Removed,
					Url = string.Empty,
					ArticleDT = DateTime.Now,
					RawTitle = string.Empty,
					RawBody = string.Empty,
					Tags = string.Empty,
					Blog = di.blog
				};

				di.sc.Send(new SendOrPostCallback((o) =>
				{
					lock (App.dbLock)
					{
						try
						{
							App.db.Articles.Add(article);
							App.db.SaveChanges();
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
										Debug.WriteLine("Fail to insert empty article No: {0}. release semaphore", di.ItemNo);
										semaphore.Release();
										if (ArticlesLoadProgressStepStage2 != null)
											ArticlesLoadProgressStepStage2();
									}
								}
							}
							else
							{
								Debug.WriteLine("Task for item No {0}: release semaphore (db update fail)", di.ItemNo);
								semaphore.Release();
								if (ArticlesLoadProgressStepStage2 != null)
									ArticlesLoadProgressStepStage2();

								throw e;
							}
						}
					}
				}), null);
			}
			else
			{
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
				//ManualResetEvent evt = new ManualResetEvent(false);
				di.sc.Send(new SendOrPostCallback((o) =>
				{
					lock (App.dbLock)
					{
						try
						{
							App.db.Articles.Add(article);
							App.db.SaveChanges();
							//evt.Set();
							di.ShouldProcessPage = true;
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
										Debug.WriteLine("Fail to insert article No: {0}. release semaphore", di.ItemNo);
										semaphore.Release();
										if (ArticlesLoadProgressStepStage2 != null)
											ArticlesLoadProgressStepStage2();
									}
								}
							}
							else
							{
								Debug.WriteLine("Task for item No {0}: release semaphore (db update fail)", di.ItemNo);
								semaphore.Release();
								if (ArticlesLoadProgressStepStage2 != null)
									ArticlesLoadProgressStepStage2();

								throw e;
							}
						}
					}
				}), null);

				if (di.ShouldProcessPage)
				{
					HtmlLoader loaderStage2 = new HtmlLoader(article, di.sc, this);
				}
			}
		}
		public void SaveArticleDetails(Article article, List<Picture> pictures)
		{
			semaphore.Release();
			lock (App.dbLock)
			{
				var context = App.db;
				context.Entry(article).State = EntityState.Modified;
				context.SaveChanges();
				if (pictures != null)
				{
					foreach (var p in pictures)
						context.Pictures.Add(p);
					context.SaveChanges();
				}
				if (ArticlesLoadProgressStepStage2 != null)
					ArticlesLoadProgressStepStage2();
			}
		}
	}

	class HtmlLoader
	{
		Article Article;
		CefSharp.OffScreen.ChromiumWebBrowser browser;
		SynchronizationContext syncContext;
		//ManualResetEvent evt;
		DownloadManager downloadManager;
		public HtmlLoader(Article _article, SynchronizationContext _syncContext, DownloadManager _downloadManager)
		{
			Article = _article;
			syncContext = _syncContext;
			downloadManager = _downloadManager;
			browser = new CefSharp.OffScreen.ChromiumWebBrowser();
			browser.BrowserInitialized += Browser_BrowserInitialized;
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
		}
		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("Thread {0}: Start process event '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
			browser.Load(Article.Url);
		}
		private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
		{
			if (e.Url.StartsWith("https://www.livejournal.com/login.bml"))
				SavePageToDB(false);
			if (e.Url != Article.Url)
				return;
			ExtractArticleTitle(1);
		}
		private async void ExtractArticleTitle(int tagNo)
		{
			string TagName = string.Empty;
			switch (tagNo)
			{
				case 1:
					TagName = "aentry-post__title-text";
					break;
				case 2:
					TagName = "entry-title";
					break;
				default:
					ExtractArticleBody(1);
					return;
			}

			string script = string.Format("(function() {{ var x = document.getElementsByClassName('{0}'); return x.length > 0 ? x[0].innerHTML : '';}} )();", TagName);
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					Article.RawTitle = response.Success ? response.Result.ToString().Trim() : string.Empty;
					if (Article.RawTitle.Length == 0)
						ExtractArticleTitle(tagNo + 1);
					else
						ExtractArticleBody(1);
				}
				else
					SavePageToDB(false);
			});
		}
		private async void ExtractArticleBody(int tagNo)
		{
			string TagName = string.Empty;
			switch (tagNo)
			{
				case 1:
					TagName = "aentry-post__text";
					break;
				case 2:
					TagName = "b-singlepost-body";
					break;
				default:
					SavePageToDB(false);
					return;
			}

			string script = string.Format("(function() {{ var x = document.getElementsByClassName('{0}'); return x.length > 0 ? x[0].innerHTML : '';}} )();", TagName);
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					Article.RawBody = response.Success ? response.Result.ToString() : string.Empty;
					if (Article.RawBody.Length == 0)
						ExtractArticleBody(tagNo + 1);
					else
						ExtractImageList(1);
				}
				else
					SavePageToDB(false);
			});
		}
		private string[] Images;
		private async void ExtractImageList(int tagNo)
		{
			string TagName = string.Empty;
			switch (tagNo)
			{
				case 1:
					TagName = "aentry-post__text";
					break;
				case 2:
					TagName = "b-singlepost-body";
					break;
				default:
					SavePageToDB();
					return;
			}

			string script = string.Format("(function() {{ var imgs = document.getElementsByClassName('{0}')[0].getElementsByTagName('img'); var sImgs = '&'; for (var i = 0; i < imgs.length; i++) {{ sImgs += (imgs[i].src + '&');}} return sImgs;}} )();", TagName);

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
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
						DownloadPictures();
					}
					else
						ExtractImageList(tagNo + 1);
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
			catch(System.Net.WebException /*e*/)
			{
				//Debug.WriteLine("DownloadRemoteImageFile: got exception: " + e.Message);
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

			Debug.WriteLine(string.Format("Thread {0}: SavePageToDB: succeed = {1}, url = '{2}'", Thread.CurrentThread.ManagedThreadId, Success.ToString(), this.Article.Url));
			syncContext.Post(new SendOrPostCallback((o) =>
			{
				Article.State = Success ? ArticleState.Ready : ArticleState.FailedToProcess;
				downloadManager.SaveArticleDetails(Article, Pictures);
			}), null);
		}
	}
}
