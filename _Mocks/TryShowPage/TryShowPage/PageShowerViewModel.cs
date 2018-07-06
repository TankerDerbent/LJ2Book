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
			TextAddress = @"https://testdev666.livejournal.com/1496.html";// article 5: with youtube

		}
		private void LoadRawPage()
		{
			Debug.WriteLine("ThreadID = {0}, LoadRawPage", Thread.CurrentThread.ManagedThreadId);
			browser = new ChromiumWebBrowser();
			browser.BrowserInitialized += Browser_BrowserInitialized;
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
		}

		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			Debug.WriteLine("ThreadID = {0}, Browser_FrameLoadEnd", Thread.CurrentThread.ManagedThreadId);
			if (e.Url != TextAddress)
				return;
			ExtractArticleTitle();
		}
		private string Title = string.Empty;
		private string Body = string.Empty;
		private string[] Images;
		
		private async void ExtractArticleTitle()
		{
			Debug.WriteLine("Task 'GetTitle - start' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
			var task = browser.EvaluateScriptAsync("(function() {{ var x = document.getElementsByClassName('aentry-post__title-text'); return x.length > 0 ? x[0].innerHTML : '';}} )();");
			await task.ContinueWith(t =>
			{
				Debug.WriteLine("Task 'GetTitle - ContinueWith' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
				if (!t.IsFaulted)
				{
					var response = t.Result;
					//if (response.Success)
					//	Title = response.Result.ToString();
					Title = response.Success ? response.Result.ToString() : string.Empty;
					Debug.WriteLine(string.Format("Got title: '{0}', going to take titled body", Title));
					ExtractArticleBody();
				}
			});
		}
		private async void ExtractArticleBody()
		{
			Debug.WriteLine("Task '{0} - start' ThreadID = {1}", Title == string.Empty ? "GetBody woT" : "GetBody w/T", Thread.CurrentThread.ManagedThreadId);
			string script = "(function() {{ var x = document.getElementsByClassName('aentry-post__text'); return x.length > 0 ? x[0].innerHTML : '';}} )();";
			if (Title.Length < 1)
				script = script.Replace("aentry-post__text", "b-singlepost-body");

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				Debug.WriteLine("Task 'GetBody - ContinueWith' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
				if (!t.IsFaulted)
				{
					var response = t.Result;
					//var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
					//Body = EvaluateJavaScriptResult.ToString();
					Body = response.Success ? response.Result.ToString() : string.Empty;
					Debug.WriteLine(string.Format("Got body: '{0}'", Body));
					ExtractImageList();
				}
			});
		}
		private async void ExtractImageList()
		{
			Debug.WriteLine("Task 'GetImagesList - Start' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
			string script = "(function() {{ var imgs = document.getElementsByClassName('aentry-post__text')[0].getElementsByTagName('img'); var sImgs = '&'; for (var i = 0; i < imgs.length; i++) { sImgs += (imgs[i].src + '&');} return sImgs;}} )();";
			if (Title.Length < 1)
				script = script.Replace("aentry-post__text", "b-singlepost-body");

			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				Debug.WriteLine("Task 'GetImagesList - ContinueWith' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
				if (!t.IsFaulted)
				{
					var response = t.Result;
					var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
					List<string> result = new List<string>();
					foreach (var s in EvaluateJavaScriptResult.ToString().Split('&'))
						if (s.Length > 0)
							result.Add(s);
					Images = result.ToArray();
					string resultStr = string.Join("\r\n", EvaluateJavaScriptResult.ToString().Split('&'));
					Debug.WriteLine(string.Format("Got images list: \r\n{0}", resultStr));
					BuildAndShowPage();
				}
			});
		}

		private void BuildAndShowPage()
		{
			Debug.WriteLine("Task 'BuildAndShowPage - start' ThreadID = " + Thread.CurrentThread.ManagedThreadId);
			//string myUrl = "file://smth";

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
		}

		private void Browser_BrowserInitialized(object sender, EventArgs e)
		{
			Debug.WriteLine("ThreadID = {0}, Browser_BrowserInitialized", Thread.CurrentThread.ManagedThreadId);
			browser.Load(TextAddress);
		}

		public override void Dispose()
		{
			//throw new NotImplementedException();
		}
	}
}
