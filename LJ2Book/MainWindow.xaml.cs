using System.Windows;
using System.Windows.Input;

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
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.Escape)
			{
				if (this.DataContext is MainWindowViewModel)
				{
					if ((this.DataContext as MainWindowViewModel).Mode == MainWindowViewModel.EMode.Login)
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
						case MainWindowViewModel.EMode.BrowseStorage:
							(ctrlBrowseStorage.DataContext as FormBrowseStorage.BrowseStorageViewModel).DoEnter(this);
							break;
						case MainWindowViewModel.EMode.Login:
							(ctrlLogin.DataContext as FormLogin.LoginViewModel).DoEnter();
							break;
					}
				}
			}
		}
	}
}
