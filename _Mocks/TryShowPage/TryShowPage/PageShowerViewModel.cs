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
			//TextAddress = @"https://evo-lutio.livejournal.com/32020.html";// evo-lutio: Физкультура
			TextAddress = @"https://evo-lutio.livejournal.com/47545.html";

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
		}

		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("ThreadID = {0}, Browser_BrowserInitialized", Thread.CurrentThread.ManagedThreadId);
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
		private async void ExtractImageList()
		{
			//var articles = document.getElementsByClassName('aentry-post__text');
			//if (articles.length < 1) articles = document.getElementsByClassName('b-singlepost-body');
			//if (articles.length < 1) return ''
			string script = @"(function() {{
var imgs = document.getElementsByTagName('img');
var sImgs = ' ';
for (var i = 0; i < imgs.length; i++)
	sImgs += (imgs[i].src + ' ');
return sImgs;}} )();";

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					var resultFromJs = response.Success ? (response.Result ?? "null") : response.Message;
					if (response.Success)
					{
						List<string> result = new List<string>();
						foreach (var s in resultFromJs.ToString().Split(' '))
							if (s.Length > 0)
								result.Add(s);
						Images = result.ToArray();
						string resultStr = string.Join("\r\n", resultFromJs.ToString().Split(' '));
					}
					DownloadPictures();
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
			string response = "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n\t<meta charset=\"utf-8\">\r\n</head>\r\n<body background='#FFFFFF'>\r\n"
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
