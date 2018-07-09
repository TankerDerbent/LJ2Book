using CefSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TryProcessPage
{
	public class DownloadManager
	{
		private string _Url;
		private class DownloadManagerTaskInfo
		{
			//public string Target;
			//public int ItemNo;
			public string url;
			public SynchronizationContext sc;
			//public Blog blog;
		}
		private DownloadManagerTaskInfo di0;
		public DownloadManager(string _url)
		{
			_Url = _url;
			di0 = new DownloadManagerTaskInfo { url = _Url, sc = SynchronizationContext.Current };
			ThreadPool.QueueUserWorkItem(DownloadThread, di0);
		}
		private void DownloadThread(Object _di)
		{
			if (!(_di is DownloadManagerTaskInfo))
				return;

			//semaphore.WaitOne();

			DownloadManagerTaskInfo di = _di as DownloadManagerTaskInfo;

			//Debug.WriteLine("Task {0}: started", di.ItemNo);

			//LiveJournalEvent ev;
			//try
			//{
			//	lock (cnnSyncObject)
			//	{
			//		ev = cnn.GetEventByNo(di.Target, di.ItemNo);
			//		//Debug.WriteLine("DWM: Task {0}: for target {1} got event {2}, tags '{3}'", di.ItemNo, di.Target, ev.url, ev.Params.ContainsKey("taglist") ? ev.Params["taglist"] : string.Empty);
			//	}
			//}
			//catch (FailedToGetEventByNoException e)
			//{
			//	Debug.WriteLine(e.Message);
			//	semaphore.Release();
			//	if (ArticlesLoadProgressStep != null)
			//		ArticlesLoadProgressStep();
			//	return;
			//}
			//Article article = new Article
			//{
			//	ArticleNo = di.ItemNo,
			//	Anum = ev.anum,
			//	State = ArticleState.Queued,
			//	Url = ev.url,
			//	ArticleDT = ev.eventtime,
			//	RawTitle = string.Empty,
			//	RawBody = string.Empty,
			//	Tags = ev.Params.ContainsKey("taglist") ? ev.Params["taglist"].Replace(", ", ",") : string.Empty,
			//	Blog = di.blog
			//};
			Article article = new Article { State = ArticleState.Queued, Url = di.url };
			ManualResetEvent evt = new ManualResetEvent(false);
			//di.sc.Post(new SendOrPostCallback((o) =>
			//{
			//	lock (App.dbLock)
			//	{
			//		try
			//		{
			//			App.db.Articles.Add(article);
			//			App.db.SaveChanges();
			//			//Debug.WriteLine("Task {0}: article No {1} for '{2}' saved", di.ItemNo, article.AtricleNo, article.Blog.User.UserName);
			//			evt.Set();
			//		}
			//		catch (System.Data.Entity.Infrastructure.DbUpdateException e)
			//		{
			//			if (e.InnerException is System.Data.Entity.Core.UpdateException)
			//			{
			//				System.Data.Entity.Core.UpdateException e2 = (e.InnerException as System.Data.Entity.Core.UpdateException);
			//				if (e2.InnerException is System.Data.SQLite.SQLiteException)
			//				{
			//					System.Data.SQLite.SQLiteException e3 = e2.InnerException as System.Data.SQLite.SQLiteException;
			//					if (e3.ResultCode == System.Data.SQLite.SQLiteErrorCode.Constraint)
			//					{
			//						MessageBox.Show(string.Format("Fail to insert article No: '{0}'", article.ArticleNo.ToString()));
			//					}
			//				}
			//			}
			//			else
			//			{
			//				semaphore.Release();
			//				if (ArticlesLoadProgressStep != null)
			//					ArticlesLoadProgressStep();

			//				throw e;
			//			}
			//		}
			//	}
			//}), null);
			HtmlLoader loaderStage2 = new HtmlLoader(article, di.sc, evt, this);
		}
		internal void SaveArticleDetails(Article article, object pictures)
		{
			//throw new NotImplementedException();
			Debug.WriteLine("DownloadManager.SaveArticleDetails");
			if (ArticleReady != null)
				ArticleReady(article);
		}
		public delegate void OnArticleReady(Article a);
		public event OnArticleReady ArticleReady;
	}

	public enum ArticleState { Unknown, Queued, Ready, FailedToProcess }
	public class Article
	{
		private const string HIDDEN_ARTICLE_TITLE_TEXT = "Hidden article";
		private const string NO_TITLE_ARTICLE_TEXT = "(no title)";
		public string Url { get; set; }
		public string RawTitle { get; set; }
		public string RawBody { get; set; }
		public ArticleState State { get; set; }
		public string Title
		{
			get
			{
				if (this.RawTitle.Length == 0)
				{
					if (this.RawBody.Length == 0)
						return HIDDEN_ARTICLE_TITLE_TEXT;

					return NO_TITLE_ARTICLE_TEXT;
				}

				return RawTitle;
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

			browser = new CefSharp.OffScreen.ChromiumWebBrowser();
			browser.BrowserInitialized += Browser_BrowserInitialized;
			//browser.FrameLoadStart += Browser_FrameLoadStart;
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
		}

		//private void Browser_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
		//{
		//	if (GetBaseUrl(e.Url).EndsWith("rambler.ru"))
		//	{
		//		e.Frame.Delete();
		//		Debug.WriteLine("Browser_FrameLoadStart: delete frame " + e.Url);
		//	}
		//	else
		//		Debug.WriteLine("Browser_FrameLoadStart: load frame " + e.Url);
		//	//throw new NotImplementedException();
		//}

		private string[] Images;

		Regex rx = new Regex("http[s]?\\://([^/]*)/.*");
		private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
		{
			//Debug.WriteLine("Browser_FrameLoadEnd  : " + e.Url + ", base url = " + GetBaseUrl(e.Url));

			if (e.Url.StartsWith("https://www.livejournal.com/login.bml"))
				SavePageToDB(false);

			if (e.Url != Article.Url)
				return;

			//PreProcessPage();
			ExtractArticleTitle(1);
		}
		//private string GetBaseUrl(string _rawUrl)
		//{
		//	string baseUrl = string.Empty;
		//	if (_rawUrl == "about:blank")
		//		baseUrl = _rawUrl;
		//	else
		//	{
		//		Match m = rx.Match(_rawUrl);
		//		if (m.Groups.Count > 1)
		//			baseUrl = m.Groups[1].Value.ToString();
		//	}
		//	return baseUrl;
		//}
		private async void PreProcessPage()
		{
			string script =
				"var ljsales = document.getElementsByClassName('ljsale'); for (var i = 0; i < ljsales.length; i++) ljsales[i].parentNode.removeChild(ljsales[i]);";// +
				//"var iframes = document.querySelectorAll('iframe'); for (var i = 0; i < iframes.length; i++) iframes[i].parentNode.removeChild(iframes[i]);";
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t => { /*ExtractArticleTitle(1);*/ExtractArticleBody(1); });
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
					SavePageToDB(false);
					break;
			}

			string script = string.Format("(function() {{ var x = document.getElementsByClassName('{0}'); return x.length > 0 ? x[0].innerHTML : '';}} )();", TagName);
			//var task = browser.EvaluateScriptAsync("(function() {{ var x = document.getElementsByClassName('aentry-post__title-text'); return x.length > 0 ? x[0].innerHTML : '';}} )();");
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					Article.RawTitle = response.Success ? response.Result.ToString().Trim() : string.Empty;
					//Debug.WriteLine("Thread {0}: Tittle loaded: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
					if (Article.RawTitle.Length == 0)
						ExtractArticleTitle(tagNo + 1);
					else
						PreProcessPage();
						//ExtractArticleBody(1);
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
					break;
			}

			string script = string.Format("(function() {{ var x = document.getElementsByClassName('{0}'); return x.length > 0 ? x[0].innerHTML : '';}} )();", TagName);
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					Article.RawBody = response.Success ? response.Result.ToString() : string.Empty;
					//Debug.WriteLine("Thread {0}: Body loaded: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
					if (Article.RawBody.Length == 0)
						ExtractArticleBody(tagNo + 1);
					else
						//ExtractImageList();
						SavePageToDB();
				}
				else
					SavePageToDB(false);
			});
		}
		private async void ExtractImageList()
		{
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
		//private List<Picture> Pictures = new List<Picture>();
		private void DownloadPictures()
		{
			//if (Images.Count() > 0)
			//{
			//	App.db.Pictures.Load();
			//	string[] ImagesToLoad = Images.Except((from p in App.db.Pictures select p.Url).ToArray()).ToArray();
			//	foreach (var url in ImagesToLoad)
			//	{
			//		byte[] blob;
			//		if (DownloadRemoteImageFile(url, out blob))
			//			Pictures.Add(new Picture { Url = url, Data = blob });
			//	}
			//}
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
			catch (System.Net.WebException e)
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

			//evt.WaitOne();

			Debug.WriteLine("Thread {0}: Event setd: '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
			syncContext.Post(new SendOrPostCallback((o) =>
			{
				Article.State = Success ? ArticleState.Ready : ArticleState.FailedToProcess;
				downloadManager.SaveArticleDetails(Article, null);
			}), null);
		}

		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("Thread {0}: start load '{1}'", Thread.CurrentThread.ManagedThreadId, this.Article.Url);
			browser.Load(Article.Url);
		}
	}
}
