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
	sealed class DownloadManager
	{
		// Singleton
		private static DownloadManager _Instance = null;
		private static readonly object createLock = new object();
		private DownloadManager() { }
		public static DownloadManager Instance {  get { if (_Instance == null) lock (createLock) if (_Instance == null) _Instance = new DownloadManager(); return _Instance; } }
		// objects
		private static Connection cnn;
		private static object cnnSyncObject = new object();
		//public SynchronizationContext WpfSyncContext { get; set; }
		
		//public DownloadManager(SynchronizationContext _ctx)
		//{
		//	WpfSyncContext = _ctx;
		//}
		public static bool TryLogin(string _Login, string _encryptedPass)
		{
			lock (cnnSyncObject)
			{
				cnn = new Connection(_Login, _encryptedPass);
				return cnn.CheckConnection();
			}
		}
		public class BlogSynchronizationTask
		{
			// declarations 
			public delegate void OnArticlesLoadProgressChanged(int MaxItems);

			// events
			public event OnArticlesLoadProgressChanged OverallProgressChangedStage1;
			public event OnArticlesLoadProgressChanged OverallProgressChangedStage2;
			public Action StepProgressStage1;
			public Action StepProgressStage2;
			public Action BlogSummaryChanged;
			public Action SynchronizationEnded;

			private SynchronizationContext SyncContext;
			public Blog blog { get; set; }
			public const int DOWNLOAD_THREADS = 1;
			public Semaphore semaphore;
			public BlogSynchronizationTask(SynchronizationContext _SyncContext, Blog _blog)
			{
				if (_SyncContext == null && _blog == null)
					throw new InvalidOperationException("sync context cannot be null");

				SyncContext = _SyncContext;
				blog = _blog;
			}
			public int ProgressMax1
			{
				set
				{
					SyncContext.Post(new SendOrPostCallback(o =>
					{
						if (OverallProgressChangedStage1 != null)
							OverallProgressChangedStage1((int)o);
					}), value);
				}
			}
			public int ProgressMax2
			{
				set
				{
					SyncContext.Post(new SendOrPostCallback(o =>
					{
						if (OverallProgressChangedStage2 != null)
							OverallProgressChangedStage2((int)o);
					}), value);
				}
			}
			public void Step1()
			{
				SyncContext.Post(new SendOrPostCallback(o =>
				{
					if (StepProgressStage1 != null)
						StepProgressStage1();
				}), null);
			}
			public void Step2()
			{
				SyncContext.Post(new SendOrPostCallback(o =>
				{
					if (StepProgressStage2 != null)
						StepProgressStage2();
				}), null);
			}
			public void EndSynchronization()
			{
				SyncContext.Post(new SendOrPostCallback(o =>
				{
					if (SynchronizationEnded != null)
						SynchronizationEnded();
				}), null);
			}
			public void UpdateBlog()
			{
				SyncContext.Send(new SendOrPostCallback(o =>
				{
					lock (App.dbLock)
					{
						App.db.Entry(blog).State = EntityState.Modified;
						App.db.SaveChanges();
					}
					if (BlogSummaryChanged != null)
						BlogSummaryChanged();
				}), null);
			}
			public int[] GetItemNumbersToSync()
			{
				int[] result = { 1 };

				SyncContext.Send(new SendOrPostCallback(o =>
				{
					lock (App.dbLock)
					{
						App.db.Entry(blog).Collection(b => b.Articles).Load();
						if (blog.Articles == null)
						{
							result = Enumerable.Range(1, blog.LastItemNo).ToArray();
						}
						else
						{
							var qryArticles = from a in blog.Articles select a.ArticleNo;
							result = Enumerable.Range(1, blog.LastItemNo).Except(qryArticles.ToArray()).ToArray();
						}
					}
				}), null);

				return result;
			}
			public void SaveArticlesToDB(List<Article> articles)
			{
				SyncContext.Send(new SendOrPostCallback(o =>
				{
					lock (App.dbLock)
					{
						foreach (var a in articles)
							try
							{
								App.db.Articles.Add(a);
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
											Debug.WriteLine("Fail to insert empty article No: {0}, but continue.", a.ArticleNo);
										}
									}
								}
								else
								{
									Debug.WriteLine("Fail to insert empty article No: {0}, and rethrow.", a.ArticleNo);
									throw e;
								}
							}
					}
				}), null);
			}
			public Article[] GetArticlesToLoad()
			{
				Article[] result = { new Article() };

				SyncContext.Send(new SendOrPostCallback(o =>
				{
					lock (App.dbLock)
					{
						App.db.Entry(blog).Collection(b => b.Articles).Load();
						var qryArticles = from a in blog.Articles where a.State == ArticleState.Queued select a;
						result = qryArticles.ToArray();
					}
				}), null);

				return result;
			}

			public void SaveArticleDetails(Article article, List<Picture> pictures)
			{
				Debug.WriteLine(string.Format("Semaphore: Release -1"));
				semaphore.Release();
				SyncContext.Post(new SendOrPostCallback(o =>
				{
					lock (App.dbLock)
					{
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
							Step2();
						}
					}
				}), null);
			}
		}
		private List<BlogSynchronizationTask> Tasks = new List<BlogSynchronizationTask>();
		private object tasksSyncObject = new object();
		public void AddSyncTask(BlogSynchronizationTask task)
		{
			lock (tasksSyncObject)
				Tasks.Add(task);

			ThreadPool.QueueUserWorkItem(SyncBlogTask, task);
		}
		private void SyncBlogTask(object _task)
		{
			lock(cnnSyncObject)
			{
				if (cnn == null || !cnn.CheckConnection())
					throw new InvalidOperationException("lj connection error");
			}
			BlogSynchronizationTask task = (BlogSynchronizationTask)_task;

			int LastEventNo = -1;
			lock (cnnSyncObject)
				LastEventNo = cnn.GetLastEventNo(task.blog.User.UserName);

			if (LastEventNo < 0 || LastEventNo < task.blog.LastItemNo)
			{
				task.EndSynchronization();
				return;
			}

			task.blog.LastItemNo = LastEventNo;
			task.blog.LastSync = DateTime.Now;
			task.UpdateBlog();

			CollectStage1Info(task);
		}
		//public void SyncBlog(Blog _blog)
		//{
		//	if (cnn == null || !cnn.CheckConnection())
		//		throw new InvalidOperationException("lj connection error");

		//	int LastEventNo = -1;
		//	lock (cnnSyncObject)
		//		LastEventNo = cnn.GetLastEventNo(_blog.User.UserName);

		//	if (LastEventNo < 0)
		//	{
		//		MessageBox.Show("Some error, possible client locked");
		//		return;
		//	}

		//	Blog blog;
		//	lock (App.dbLock)
		//	{
		//		blog = App.db.Blogs.Find(_blog.UserBlogID);
		//		bool NeedUpdate = LastEventNo > blog.LastItemNo;
		//		blog.LastItemNo = LastEventNo;
		//		blog.LastSync = DateTime.Now;
		//		App.db.Entry(blog).State = EntityState.Modified;
		//		App.db.SaveChanges();
		//	}
		//	if (BlogInfoArrived != null)
		//		BlogInfoArrived(blog.LastItemNo, blog.LastSync);

		//	if (blog.KindOfSynchronization == KindOfSynchronization.Auto)
		//		SyncBlog2(blog);
		//}
		//public delegate void OnBlogInfoArrived(int lastItemNo, DateTime dateLastSync);
		//public event OnBlogInfoArrived BlogInfoArrived;

		private void CollectStage1Info(BlogSynchronizationTask task)
		{
			int[] ItemNumbersToSync = task.GetItemNumbersToSync();

			int NumberItemsToCollectInfo = ItemNumbersToSync.Length;
			if (NumberItemsToCollectInfo == 0)
			{
				Debug.WriteLine("Articles to load: nothing");
				task.EndSynchronization();
				return;
			}
			string logString = string.Format("Articles to load: " + string.Join(",", (from i in ItemNumbersToSync select i.ToString()).ToArray()));
			if (logString.Length > 150)
				Debug.WriteLine(logString.Substring(0, 130) + "...<skipped>..." + logString.Substring(logString.Length - 15, 15));
			else
				Debug.WriteLine(logString);

			task.ProgressMax1 = NumberItemsToCollectInfo;

			List<Stage2TaskInfo> diList = new List<Stage2TaskInfo>();
			List<Article> Articles = new List<Article>();
			int n = 0;
			foreach (var i in ItemNumbersToSync)
			{
				Thread.Sleep(205);
				Article article = new Article { ArticleNo = i, Anum = 0, Url = string.Empty, RawTitle = string.Empty, RawBody = string.Empty, Tags = string.Empty, Blog = task.blog };
				try
				{
					LiveJournalEvent Event = cnn.GetEventByNo(task.blog.User.UserName, i);
					if (Event == null)
					{
						Debug.WriteLine(string.Format("Get Event by No: event for {0} is deleted", i));
						article.Anum = 0;
						article.State = ArticleState.Removed;
						article.ArticleDT = DateTime.Now;
					}
					else
					{
						Debug.WriteLine(string.Format("Get Event by No: event for {0} has URL {1}", i, Event.url));

						article.Anum = Event.anum;
						article.State = ArticleState.Queued;
						article.Url = Event.url;
						article.ArticleDT = Event.eventtime;
						if (Event.Params.ContainsKey("taglist"))
							article.Tags = Event.Params["taglist"].Replace(", ", ",");

						diList.Add(new Stage2TaskInfo { article = article, task = task });
						n += 1;
					}
					Articles.Add(article);
					if (n > 20)
						break;
				}
				catch (FailedToGetEventByNoException e)
				{
					Debug.WriteLine(string.Format("Get Event by No: Fail to get event for {0}. Error: {1}", i, e.Message));
					break;
				}
				task.Step1();
			}

			task.SaveArticlesToDB(Articles);

			Article[] articlesToDownload = task.GetArticlesToLoad();

			int NumberItemsToDownload = articlesToDownload.Length;
			task.ProgressMax2 = NumberItemsToDownload;

			//Debug.WriteLine(string.Format("Semaphore: Create and lock +{0}", BlogSynchronizationTask.DOWNLOAD_THREADS));
			task.semaphore = new Semaphore(0, BlogSynchronizationTask.DOWNLOAD_THREADS);
			do
			{
				int Limit = diList.Count >= BlogSynchronizationTask.DOWNLOAD_THREADS ? BlogSynchronizationTask.DOWNLOAD_THREADS : diList.Count;
				for (int i = 0; i < Limit; i++)
				{
					Stage2TaskInfo s2ti = diList[0];
					ThreadPool.QueueUserWorkItem(Stage3Processor.DownloadThread, s2ti);
					diList.RemoveAt(0);
				}
				//Debug.WriteLine(string.Format("Semaphore: ctrl Release -{0}", Limit));
				task.semaphore.Release(Limit);
				Thread.Sleep(100);

				for (int i = 0; i < Limit; i++)
				{
					//Debug.WriteLine(string.Format("Semaphore: ctrl Wait..."));
					task.semaphore.WaitOne();
					//Debug.WriteLine(string.Format("Semaphore: ctrl Lock +1"));
				}
			}
			while (diList.Count > 0);
			task.EndSynchronization();
		}
		public class Stage2TaskInfo
		{
			public Article article;
			public BlogSynchronizationTask task;
		}
	}

	class Stage3Processor
	{
		public static void DownloadThread(Object _ti)
		{
			if (!(_ti is DownloadManager.Stage2TaskInfo))
				return;

			DownloadManager.Stage2TaskInfo ti = (DownloadManager.Stage2TaskInfo)_ti;

			Debug.WriteLine(string.Format("Semaphore: dw thread Wait..."));
			ti.task.semaphore.WaitOne();
			Debug.WriteLine(string.Format("Semaphore: dw thread Lock +1"));

			Debug.WriteLine("Task started for item No {0}: lock semaphore", ti.article.ArticleNo);

			Stage3Processor loaderStage3 = new Stage3Processor(ti);
		}

		CefSharp.OffScreen.ChromiumWebBrowser browser;
		DownloadManager.Stage2TaskInfo ti;
		public Stage3Processor(DownloadManager.Stage2TaskInfo _ti)
		{
			ti = _ti;
			browser = new CefSharp.OffScreen.ChromiumWebBrowser();
			browser.BrowserInitialized += Browser_BrowserInitialized;
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
		}
		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("Thread {0}: Start process event '{1}'", Thread.CurrentThread.ManagedThreadId, ti.article.Url);
			browser.Load(ti.article.Url);
		}
		private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
		{
			if (e.Url.StartsWith("https://www.livejournal.com/login.bml"))
				StopCollection(false);
			if (e.Url != ti.article.Url)
				return;
			ExtractArticleTitle();
		}
		private async void ExtractArticleTitle()
		{
			string script = @"(function() {{
var tittles = document.getElementsByClassName('aentry-post__title-text');
if (tittles.length < 1)
	tittles = document.getElementsByClassName('entry-title');
if (tittles.length > 0) 
	return tittles[0].innerHTML;
else
	return ''
 }} )();";

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					ti.article.RawTitle = response.Success ? response.Result.ToString().Trim() : string.Empty;
					ExtractArticleBody();
				}
				else
					StopCollection(false);
			});
		}
		private async void ExtractArticleBody()
		{
			string script = @"(function() {{
var articles = document.getElementsByClassName('aentry-post__text');
if (articles.length < 1)
	articles = document.getElementsByClassName('b-singlepost-body');
if (articles.length < 1)
	return ''
var theResultText = '<div class=""result-article"">' + articles[0].innerHTML; + '</div>';
document.body.innerHTML = theResultText;
var ljsales = document.getElementsByClassName('ljsale');
for (var i = 0; i < ljsales.length; i++)
	ljsales[i].parentNode.removeChild(ljsales[i]);
var ljlikes = document.getElementsByClassName('lj-like');
for (var j = 0; j < ljlikes.length; j++)
	ljlikes[j].parentNode.removeChild(ljlikes[j]);
return document.getElementsByClassName('result-article')[0].innerHTML;
 }} )();";

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					ti.article.RawBody = response.Success ? response.Result.ToString() : string.Empty;
					ExtractImageList();
				}
				else
					StopCollection(false);
			});
		}
		private string[] Images;
		private async void ExtractImageList()
		{
			string script = @"(function() {{
var imgs = document.getElementsByTagName('img');
var sImgs = '&';
for (var i = 0; i < imgs.length; i++)
	sImgs += (imgs[i].src + '&');
return sImgs;}} )();";

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
					}
					DownloadPictures();
				}
				else
					StopCollection(false);
			});
		}
		private List<Picture> Pictures = new List<Picture>();
		private void DownloadPictures()
		{
			if (Images != null && Images.Count() > 0)
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
			StopCollection();
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
		private void StopCollection(bool Success = true)
		{
			browser.BrowserInitialized -= Browser_BrowserInitialized;
			browser.FrameLoadEnd -= Browser_FrameLoadEnd;

			Debug.WriteLine(string.Format("Thread {0}: SavePageToDB: succeed = {1}, url = '{2}'", Thread.CurrentThread.ManagedThreadId, Success.ToString(), ti.article.Url));
			ti.task.SaveArticleDetails(ti.article, Pictures);
			ti.task.Step2();
		}
	}
}
