using CefSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TrySubstImage
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;

		}
		bool _Init = false;
		MemoryStream msProxyPngImage;
		string sUrlToLoad = @"file:///D:/test_png.html";
		string sImageUrl = @"https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RE1Mu3b?ver=5c31";
		private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			//throw new NotImplementedException();
			if (!(e.NewValue is bool))
				return;

			if ((bool)e.NewValue == true)
			{
				if (_Init)
					return;
				_Init = true;

				browser.FrameLoadEnd += Browser_FrameLoadEnd;
				string proxyPngFile = @"D:\2.png";
				byte[] bytesProxyPngImage = File.ReadAllBytes(proxyPngFile);
				msProxyPngImage = new MemoryStream();
				msProxyPngImage.Write(bytesProxyPngImage, 0, bytesProxyPngImage.Length);

				//browser.RegisterResourceHandler(@"file://D:/1.png", msProxyPngImage, ResourceHandler.GetMimeType(".png"));
				browser.RegisterResourceHandler(sImageUrl, msProxyPngImage, ResourceHandler.GetMimeType(".png"));
				browser.Load(sUrlToLoad);
			}
		}

		private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
		{
			Debug.WriteLine("FrameLoadEnd: " + e.Url);
			//throw new NotImplementedException();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}
	}
}
