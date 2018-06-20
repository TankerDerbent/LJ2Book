using CefSharp;
using LJ2Book.DataBase;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LJ2Book
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
		public static SiteContext db { get; internal set; }
		public static object dbLock { get; set; }

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			db = new SiteContext();
			dbLock = new object();

			MainWindow window = new MainWindow();
			window.Show();
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Cef.Shutdown();
		}
	}
}
