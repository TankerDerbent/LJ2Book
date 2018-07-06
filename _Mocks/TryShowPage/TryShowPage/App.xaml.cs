using CefSharp;
using System.Windows;

namespace TryShowPage
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			CefSettings settings = new CefSettings();
			settings.LogSeverity = LogSeverity.Disable;
			Cef.Initialize(settings);
		}
	}
}
