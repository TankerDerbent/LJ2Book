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
		}

		private void LoginControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is LoginViewModel)
				ctrlPassword.Password = (e.NewValue as LoginViewModel).UnsecurePassword;
		}

		private void ctrlPassword_PasswordChanged(object sender, RoutedEventArgs e)
		{
			(this.DataContext as LoginViewModel).UnsecurePassword = ctrlPassword.Password;
			LoginViewModel vm = (this.DataContext as LoginViewModel);
			string s = vm.UnsecurePassword;
		}
	}
}
