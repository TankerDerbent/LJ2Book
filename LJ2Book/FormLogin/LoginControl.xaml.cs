using System.Windows;
using System.Windows.Controls;

namespace LJ2Book.FormLogin
{
	/// <summary>
	/// Interaction logic for LoginControl.xaml
	/// </summary>
	public partial class LoginControl : UserControl
	{
		public LoginControl()
		{
			InitializeComponent();

			this.DataContextChanged += LoginControl_DataContextChanged;
			this.Loaded += LoginControl_Loaded;
		}

		private void LoginControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ctrlLogin.Text.Length > 0)
			{
				ctrlPassword.Focus();
				ctrlPassword.SelectAll();
			}
		}

		private void LoginControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is LoginViewModel)
			{
				LoginViewModel vm = e.NewValue as LoginViewModel;
				ctrlPassword.Password = vm.UnsecurePassword;
			}
		}

		private void ctrlPassword_PasswordChanged(object sender, RoutedEventArgs e)
		{
			(this.DataContext as LoginViewModel).UnsecurePassword = ctrlPassword.Password;
		}
	}
}
