using System.Windows;
using System.Windows.Input;
using SimplesNet;

namespace LJ2Book.FormLogin
{
	class LoginViewModel : BaseViewModel
	{
		private string _Login = "";
		private string _SecuredPassword = "";
		private LJ2Book.MainWindowViewModel RootVM;

		public LoginViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			RootVM = _RootVM;
			RememberLoginAndPass = false;
		}

		public bool RememberLoginAndPass { get; set; }
		public string Login { get => _Login; set { _Login = value; OnPropertyChanged(() => IsOkEnabled); } }
		public string UnsecurePassword
		{
			get
			{
				return SimplesNet.Protector.Unprotect(_SecuredPassword);
			}
			set
			{
				_SecuredPassword = SimplesNet.Protector.Protect(value);
				OnPropertyChanged(() => IsOkEnabled);
			}
		}
		public string SecurePassword { get => _SecuredPassword; set => _SecuredPassword = value; }
		public bool IsOkEnabled { get { return _Login.Length > 0 && _SecuredPassword.Length > 0; } }
		public ICommand LoginCommand {  get { return new BaseCommand(() => { RootVM.Mode = MainWindowViewModel.EMode.BrowseStorage; }); } }

		public override void Dispose()
		{
			//throw new NotImplementedException();
		}
	}
}
