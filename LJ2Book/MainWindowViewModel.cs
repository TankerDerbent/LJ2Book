using System;
using System.Linq;
using System.Windows;
using SimplesNet;
using LJ2Book.DataBase;

namespace LJ2Book
{
	class MainWindowViewModel : BaseViewModel
	{
		private readonly string DO_REMEMBER_USER = "RememberUser";
		private readonly string REMEMBERED_USER_ID = "RememberedUserID";
		public enum EMode { Login, BrowseStorage }
		private EMode _Mode = EMode.Login;
		public LJ2Book.FormLogin.LoginViewModel LoginVM { get; set; }
		public LJ2Book.FormBrowseStorage.BrowseStorageViewModel BrowseStorageVM { get; set; }
		private bool RememberLoginAndPass { get => LoginVM.RememberLoginAndPass; }
		public SiteContext db { get; internal set; }
		public MainWindowViewModel()
		{
			db = new SiteContext();
			LoginVM = new FormLogin.LoginViewModel(this);
			BrowseStorageVM = new FormBrowseStorage.BrowseStorageViewModel(this);

			using (var context = new SiteContext("DefaultConnection"))
			{
				if (context.Params.Count() > 0)
				{
					LoginVM.RememberLoginAndPass = Param.GetParamBool(DO_REMEMBER_USER, context);

					if (RememberLoginAndPass)
					{
						int UserID = Param.GetParamInt(REMEMBERED_USER_ID, context);

						User user = context.Users.Find(UserID);
						if (user != null)
						{
							LoginVM.Login = user.UserName;
							LoginVM.SecurePassword = user.Password;
						}
					}
				}
			}
		}
		public Visibility LoginControlVisibility
		{
			get
			{
				return Mode == EMode.Login ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public Visibility BrowseStorageControlVisibility
		{
			get
			{
				return Mode == EMode.BrowseStorage ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public EMode Mode
		{
			get
			{
				return _Mode;
			}
			set
			{
				ProcessLoginForm();
				_Mode = value;
				OnPropertyChanged(() => LoginControlVisibility);
				OnPropertyChanged(() => BrowseStorageControlVisibility);
			}
		}

		private void ProcessLoginForm()
		{
			using (var context = new SiteContext("DefaultConnection"))
			{
				Param.SetParam(DO_REMEMBER_USER, RememberLoginAndPass, context);

				if (RememberLoginAndPass)
				{
					int UserID = -1;
					var userQry = from u in context.Users where u.UserName.ToLower() == LoginVM.Login.ToLower() select u;
					if (userQry.Count() == 1)
					{
						User user = userQry.First();
						if (LoginVM.SecurePassword != user.Password)
						{
							user.Password = LoginVM.SecurePassword;
							context.Entry(user).State = System.Data.Entity.EntityState.Modified;
							context.SaveChanges();
							UserID = user.UserID;
						}
					}
					else if (userQry.Count() > 1)
					{
						throw new InvalidOperationException(string.Format("number of users, named '{0}' is {1}!", LoginVM.Login, userQry.Count()));
					}
					else// if userQry.Count() == 0
					{
						User user = new User { UserName = LoginVM.Login, Password = LoginVM.SecurePassword, UserType = UserType.LjUser };
						context.Users.Add(user);
						context.SaveChanges();
						UserID = user.UserID;
					}

					Param.SetParam(REMEMBERED_USER_ID, UserID, context);
				}
			}
		}

		public override void Dispose()
		{
			//throw new NotImplementedException();
		}
	}
}
