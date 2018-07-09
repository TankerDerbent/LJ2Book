using CefSharp;
using System.Windows;

namespace TryProcessPage
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			Cef.Initialize();
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Cef.Shutdown();
		}
	}
}
