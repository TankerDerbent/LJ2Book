using System;
using System.Windows;
using System.Windows.Input;
using SimplesNet;
using CefSharp;
using CefSharp.OffScreen;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;

namespace TryShowPage
{
	class PageShowerViewModel : BaseViewModel
	{
		private MainWindow _owner;
		ChromiumWebBrowser browser;
		public string TextAddress { get; set; }
		public ICommand ShowPageCommand
		{
			get
			{
				return new BaseCommand(() =>
				{
					IsButtonEnabled = false;
					LoadRawPage();
				});
			}
		}
		public ICommand EnableShowButton
		{
			get
			{
				return new BaseCommand(() =>
				{
					IsButtonEnabled = true;
					OnPropertyChanged(() => IsButtonEnabled);
				});
			}
		}
		public string BrowserAddress { get; internal set; }
		public bool IsButtonEnabled { get; internal set; }
		public PageShowerViewModel(Window window = null) : base(window)
		{
			_owner = (window as MainWindow);
			IsButtonEnabled = true;
			//TextAddress = @"https://testdev666.livejournal.com/397.html";// article 1
			//TextAddress = @"https://testdev666.livejournal.com/552.html";// article 1: 2 pics with cut
			//TextAddress = @"https://testdev666.livejournal.com/919.html";// article 3: hidden
			//TextAddress = @"https://testdev666.livejournal.com/1274.html";// article 4: hidden
			//TextAddress = @"https://testdev666.livejournal.com/1496.html";// article 5: with youtube
			//TextAddress = @"file:///D:/1496_full.html";
			//TextAddress = @"file:///D:/test_png.html";
			TextAddress = @"https://evo-lutio.livejournal.com/32020.html";// evo-lutio: Физкультура

			ti = new Stage2TaskInfo();
			ti.article = new Article
			{
				ArticleID = -1,
				ArticleNo = -1,
				Anum = -1,
				State = ArticleState.Unknown,
				Url = TextAddress,
				ArticleDT = DateTime.MinValue,
				RawTitle = string.Empty,
				RawBody = string.Empty,
				Tags = string.Empty
			};

			//string proxyPngFile = @"D:\2.png";
			//bytesProxyPngImage = File.ReadAllBytes(proxyPngFile);

			//msProxyPngImage = new MemoryStream();
			//msProxyPngImage.Write(bytesProxyPngImage, 0, bytesProxyPngImage.Length);
		}
		//private byte[] bytesProxyPngImage;
		//private MemoryStream msProxyPngImage;
		private void LoadRawPage()
		{
			Debug.WriteLine("ThreadID = {0}, LoadRawPage", Thread.CurrentThread.ManagedThreadId);
			browser = new ChromiumWebBrowser();
			browser.BrowserInitialized += Browser_BrowserInitialized;
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
			//browser.FrameLoadStart += Browser_FrameLoadStart;
			//ILoadHandler handler = browser.LoadHandler;
			//handler.OnFrameLoadStart()
			
		}

		//private void Browser_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
		//{
		//	Debug.WriteLine("ThreadID = {0}, Browser_FrameLoadStart TransitionType={1} '{2}'", Thread.CurrentThread.ManagedThreadId, e.TransitionType.ToString(), e.Url);
		//	if (e.Url.Contains("rubiconproject.com"))
		//		e.Frame.Delete();
		//	//throw new NotImplementedException();
		//}

		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("ThreadID = {0}, Browser_BrowserInitialized", Thread.CurrentThread.ManagedThreadId);
			//browser.RegisterResourceHandler("1.png", msProxyPngImage, ResourceHandler.GetMimeType(".png"));
			//ILoadHandler
			browser.Load(TextAddress);
		}
		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			Debug.WriteLine("ThreadID = {0}, Browser_FrameLoadEnd '{1}'", Thread.CurrentThread.ManagedThreadId, e.Url);
			if (e.Url == TextAddress)
			{
				browser.Stop();
				BeginProcessPage();
			}
		}

		private string Title = string.Empty;
		private string Body = string.Empty;
		private string[] Images;

		public enum ArticleState { Unknown, Queued, Ready, FailedToProcess, Removed }

		public class Article
		{
			public int ArticleID { get; set; }
			public int ArticleNo { get; set; }
			public int Anum { get; set; }
			public ArticleState State { get; set; }
			public string Url { get; set; }
			public DateTime ArticleDT { get; set; }
			public string RawTitle { get; set; }
			public string RawBody { get; set; }
			public string Tags { get; set; }
			//public virtual Blog Blog { get; set; }
		}

		public class Stage2TaskInfo
		{
			public Article article;
			//public BlogSynchronizationTask task;
		}

		Stage2TaskInfo ti;
		private void BeginProcessPage()
		{
			ClearRedundantTags();
		}
		private async void ClearRedundantTags()
		{
			string script = "(function() {{ " +
				"var ljsales = document.getElementsByClassName('ljsale'); for (var i = 0; i < ljsales.length; i++) ljsales[i].parentNode.removeChild(ljsales[i]);" +
				"var ljlikes = document.getElementsByClassName('lj-like'); for (var j = 0; j < ljlikes.length; j++) ljlikes[j].parentNode.removeChild(ljlikes[j]);" +
				"var ljsales_inner = document.getElementsByClassName('xhtml_banner'); for (var k = 0; k < ljsales_inner.length; k++) ljsales_inner[k].parentNode.removeChild(ljsales_inner[k]);" +
				" }} )();";
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					//ti.article.RawTitle = response.Success ? response.Result.ToString().Trim() : string.Empty;
				}
				ExtractArticleTitle(1);
			});

			//ExtractArticleTitle(1);
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
					ti.article.RawTitle = response.Success ? response.Result.ToString().Trim() : string.Empty;
					if (ti.article.RawTitle.Length == 0)
						ExtractArticleTitle(tagNo + 1);
					else
						ExtractArticleBody(1);
				}
				else
					StopCollection(false);
			});
		}
		private string ResultTag = string.Empty;
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
					ResultTag = string.Empty;
					StopCollection(false);
					return;
			}
			ResultTag = TagName;

			string script = string.Format("(function() {{ var x = document.getElementsByClassName('{0}'); return x.length > 0 ? x[0].innerHTML : '';}} )();", TagName);
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					ti.article.RawBody = response.Success ? response.Result.ToString() : string.Empty;
					if (ti.article.RawBody.Length == 0)
						ExtractArticleBody(tagNo + 1);
					else
						ExtractImageList(1);
				}
				else
					StopCollection(false);
			});
		}
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
					//StopCollection();
					DownloadPictures();
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
					StopCollection(false);
			});
		}
		private void DownloadPictures()
		{
			StopCollection();
		}

		private void StopCollection(bool v = true)
		{
			if (v)
				BuildAndShowPage();
			else
				MessageBox.Show("boroda");
		}
		private void BuildAndShowPage()
		{
			Debug.WriteLine("Task 'BuildAndShowPage - start' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
			browser.Stop();

			Title = ti.article.RawTitle;
			Body = ti.article.RawBody;

			//var factory = browser.ResourceHandlerFactory;
			//if (factory == null)
			//	return;
			string response = "<!DOCTYPE html>"
				+ "<head><meta charset=\"utf-8\"></head>"
				+ "<body>"
				+ "<h1>"
				+ Title
				+ "</h1>"
				+ "<br>"
				+ Body
				+ "</body>";
			//IResourceHandlerFactory
			//factory.RegisterHandler(myUrl, ResourceHandler.FromString(response));
			//browser.LoadHtml(response);
			_owner.Dispatcher.BeginInvoke(new Action(delegate()
			{
				Debug.WriteLine("Task 'BuildAndShowPage - BeginInvoke' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
				Debug.WriteLine(string.Format("Show html in browserOnForm: \r\n{0}", response.Substring(0, 100)));
				_owner.browserOnForm.LoadHtml(response);
			}));
			Debug.WriteLine("------------------ done ---------------------");
		}

		public override void Dispose()
		{
			//throw new NotImplementedException();
		}
	}
}
