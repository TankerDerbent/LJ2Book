using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CefSharp;
using CefSharp.OffScreen;
using SimplesNet;

namespace TryProcessPage
{
	class MainWindowViewModel : BaseViewModel
	{
		private const string _Url = @"https://evo-lutio.livejournal.com/32020.html";
		//ChromiumWebBrowser browser;
		//MainWindow window;
		public MainWindowViewModel(MainWindow _window) : base(_window)
		{
			(Window as MainWindow).browserOnForm.IsBrowserInitializedChanged += BrowserOnForm_IsBrowserInitializedChanged;
		}

		private void BrowserOnForm_IsBrowserInitializedChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue == true)
			{
				//(Window as MainWindow).browserOnForm.Load(_Url);
				LoadArticle();
			}
		}

		private void LoadArticle()
		{
			DownloadManager mgr = new DownloadManager(_Url);
			mgr.ArticleReady += Mgr_ArticleReady;
		}

		private void Mgr_ArticleReady(Article a)
		{
			//throw new NotImplementedException();
			BuildTextAndPreparePictures(a);
			(Window as MainWindow).browserOnForm.LoadHtml(_TextToShow, true);
		}
		private string _TextToShow;
		private void BuildTextAndPreparePictures(Article a)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<!DOCTYPE html>\r\n<html>\r\n<head>\r\n<meta charset=\"utf-8\">");
			sb.Append("</head>\r\n<body>");
			int LabelNo = 1;
			//List<string> imagesUrls = new List<string>();
			//_CachedImages = new List<CachedImage>();
			Regex rxImageTag = new Regex("<img[^<]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Regex rxImageUrl = new Regex("src=\\\"([^\\\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			//foreach (var a in Articles)
			{
				sb.Append(string.Format("<a name='article_{0}'>", LabelNo));
				sb.Append(string.Format("<H1>{0}. ", LabelNo));
				LabelNo += 1;
				sb.Append(a.Title);
				sb.Append("</H1></a>\r\n");
				sb.Append(a.RawBody);
				sb.Append("\r\n");

				//MatchCollection matches = rxImageTag.Matches(a.RawBody);
				//foreach (Match match in matches)
				//{
				//	Match matchUrl = rxImageUrl.Match(match.Groups[0].Value.ToString());
				//	string url = matchUrl.Groups[1].Value.ToString();
				//	var picture = App.db.Pictures.Find(new string[] { url });
				//	if (picture != null)
				//	{
				//		Debug.WriteLine(string.Format("Found cached img: '{0}'", url));
				//		MemoryStream msImage = new MemoryStream();
				//		msImage.Write(picture.Data, 0, picture.Data.Length);
				//		_CachedImages.Add(new CachedImage { Url = url, imageStream = msImage });
				//	}
				//	else
				//	{
				//		Debug.WriteLine(string.Format("Cached img: '{0}' not found", url));
				//	}
				//}
			}
			//sb.Append("<script type=\"text/javascript\">var ljsales = document.getElementsByClassName('ljsale'); for (var i = 0; i < ljsales.length; i++) ljsales[i].parentNode.removeChild(ljsales[i]);</script>");
			sb.Append("\r\n</body>\r\n</html>");
			_TextToShow = sb.ToString();
		}

		public override void Dispose()
		{
			//throw new System.NotImplementedException();
		}
	}
}
