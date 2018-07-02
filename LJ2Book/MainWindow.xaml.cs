using System.Windows;
using System.Windows.Input;
using CefSharp;

namespace LJ2Book
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

			this.Loaded += MainWindow_Loaded;

			if (!Cef.IsInitialized)
			{
				CefSettings settings = new CefSettings();
				settings.LogSeverity = LogSeverity.Disable;
				Cef.Initialize(settings);
			}
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			(this.DataContext as MainWindowViewModel).HeaderWin10StyleVM = new SimpleForms.ucHeaderWin10StyleVM(this);
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.Escape)
			{
				if (this.DataContext is MainWindowViewModel)
				{
					if ((this.DataContext as MainWindowViewModel).Mode == MainWindowViewModel.MainWindowMode.EnterLoginAndPass)
					{
						if ((ctrlLogin.DataContext as FormLogin.LoginViewModel).WorkOffline.CanExecute(null))
							(ctrlLogin.DataContext as FormLogin.LoginViewModel).WorkOffline.Execute(null);
					}
					else
					{
						Close();
					}
				}
			}

			if (e.Key == Key.Enter)
			{
				if (this.DataContext is MainWindowViewModel)
				{
					switch ((this.DataContext as MainWindowViewModel).Mode)
					{
						case MainWindowViewModel.MainWindowMode.BrowseStorage:
							(ctrlBrowseStorage.DataContext as FormBrowseStorage.BrowseStorageViewModel).DoEnter(this);
							break;
						case MainWindowViewModel.MainWindowMode.EnterLoginAndPass:
							(ctrlLogin.DataContext as FormLogin.LoginViewModel).DoEnter();
							break;
					}
				}
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
