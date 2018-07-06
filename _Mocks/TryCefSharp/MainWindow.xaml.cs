using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CefSharp;
using CefSharp.OffScreen;

namespace TryCefSharp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			//browser.Content = "123";
			//browser.Address = @"http://www.ru/";
			browser = new CefSharp.OffScreen.ChromiumWebBrowser();
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
			browser.BrowserInitialized += Browser_BrowserInitialized;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}
		private string _Url = @"https://testdev666.livejournal.com/552.html";
		ChromiumWebBrowser browser;
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			browser.Load(_Url);
		}

		private void Browser_BrowserInitialized(object sender, System.EventArgs e)
		{
			btnGo.Dispatcher.BeginInvoke(new Action(delegate ()
			{
				btnGo.IsEnabled = true;
			}));
		}

		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			if (e.Url == _Url)
			{
				//Debug.WriteLine(string.Format("Frame Loaded: URL='{0}', going to save source...", e.Url));
				//GetSourcesAsync();
				Debug.WriteLine(string.Format("Frame Loaded: URL='{0}', going to retrive DOM elements...", e.Url));

				GetDOMElement("aentry-post__title-text");
				//DumpHeader();
				GetDOMElement("aentry-post__text");
				GetAllImageLinks();
			}
			else
				Debug.WriteLine(string.Format("Frame Loaded: URL='{0}'", e.Url));
		}

		private async void GetDOMElement(string _elementName)
		{
			const string Format = "(function() {{ var x = document.getElementsByClassName('{0}'); return x[0].innerHTML;}} )();";
			string script = string.Format(Format, _elementName);
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
					Debug.WriteLine(string.Format("Got response: '{0}'", EvaluateJavaScriptResult));
				}
			});
		}
		private async void GetAllImageLinks()
		{
			const string script = "(function() {{ var imgs = document.getElementsByClassName('aentry-post__text')[0].getElementsByTagName('img'); var sImgs = '&'; for (var i = 0; i < imgs.length; i++) { sImgs += (imgs[i].src + '&');} return sImgs;}} )();";
			
			var task = browser.EvaluateScriptAsync(script);
			await task.ContinueWith(t =>
			{
				if (!t.IsFaulted)
				{
					var response = t.Result;
					var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
					string resultStr = string.Join("\r\n", EvaluateJavaScriptResult.ToString().Split('&'));
					Debug.WriteLine(string.Format("Got images list: \r\n{0}", resultStr));
					foreach (var s in EvaluateJavaScriptResult.ToString().Split('&'))
					{
						if (s.Length < 1)
							continue;
						string sFileName = "D:\\" + s.Substring(s.LastIndexOf('/') + 1);
						MainWindow.DownloadRemoteImageFile(s, sFileName);
						Debug.WriteLine(string.Format("Image {0} saved to {1}", s, sFileName));
						//img1st.Dispatcher.BeginInvoke(new Action(delegate ()
						//{
						//	img1st.Source = MainWindow.LoadImageFromUrl(s);
						//}));

						//Debug.WriteLine(string.Format("Image {0} printed to img1st", s));
						break;
					}
				}
			});
		}

		private static BitmapImage LoadImageFromUrl(string _url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if ((response.StatusCode == HttpStatusCode.OK ||
				response.StatusCode == HttpStatusCode.Moved ||
				response.StatusCode == HttpStatusCode.Redirect) &&
				response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
			{
				var image = new BitmapImage();
				// if the remote file was found, download oit
				using (Stream inputStream = response.GetResponseStream())
				//using (Stream outputStream = File.OpenWrite(fileName))
				using (MemoryStream outputStream = new MemoryStream())
				{
					byte[] buffer = new byte[4096];
					int bytesRead;
					do
					{
						bytesRead = inputStream.Read(buffer, 0, buffer.Length);
						outputStream.Write(buffer, 0, bytesRead);
					} while (bytesRead != 0);

					
					outputStream.Position = 0;
					image.BeginInit();
					image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
					image.CacheOption = BitmapCacheOption.OnLoad;
					image.UriSource = null;
					image.StreamSource = outputStream;
					image.EndInit();
				}
				image.Freeze();
				return image;
			}

			return new BitmapImage();
		}

		private static BitmapImage LoadImage(byte[] imageData)
		{
			if (imageData == null || imageData.Length == 0) return null;
			var image = new BitmapImage();
			using (var mem = new MemoryStream(imageData))
			{
				mem.Position = 0;
				image.BeginInit();
				image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.UriSource = null;
				image.StreamSource = mem;
				image.EndInit();
			}
			image.Freeze();
			return image;
		}

		private static void DownloadRemoteImageFile(string uri, string fileName)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

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
				using (Stream outputStream = File.OpenWrite(fileName))
				{
					byte[] buffer = new byte[4096];
					int bytesRead;
					do
					{
						bytesRead = inputStream.Read(buffer, 0, buffer.Length);
						outputStream.Write(buffer, 0, bytesRead);
					} while (bytesRead != 0);
				}
			}
		}

		//private void DumpDOMElement(string _elementName)
		//{
		//	const string Format = "(function() {{ var x = document.getElementsByClassName('{0}'); return x[0].innerHTML;}} )();";
		//	string script = string.Format(Format, _elementName);
		//	string elementText = CallJavascriptWithResult(script);
		//	Debug.WriteLine(string.Format("Got DOM element: '{0}' = '{1}'", _elementName, elementText));
		//}
		//private async string CallJavascriptWithResult(string _script)
		//{
		//	var task = browser.EvaluateScriptAsync(_script);
		//	string s = "";
		//	await task.ContinueWith(t =>
		//	{
		//		if (!t.IsFaulted)
		//		{
		//			var response = t.Result;
		//			var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
		//			s = EvaluateJavaScriptResult.ToString();
		//			//string resultStr = string.Join("\r\n", EvaluateJavaScriptResult.ToString().Split('&'));
		//			//Debug.WriteLine(string.Format("Got images list: \r\n{0}", resultStr));
		//		}
		//	});
		//	return s;
		//}

		private async void GetSourcesAsync()
		{
			string source = await browser.GetSourceAsync();
			File.WriteAllText(@"D:\552.htm", source);
			Debug.WriteLine(@"Source saved to D:\552.htm");
		}
	}
}
