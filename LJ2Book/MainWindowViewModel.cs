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
		public enum MainWindowMode { EnterLoginAndPass, CheckLoginAndPass, BrowseStorage, ReadBlog }
		private MainWindowMode _Mode = MainWindowMode.EnterLoginAndPass;
		private LJ2Book.SimpleForms.ucHeaderWin10StyleVM _HeaderWin10StyleVM;
		public LJ2Book.SimpleForms.ucHeaderWin10StyleVM HeaderWin10StyleVM
		{
			get { return _HeaderWin10StyleVM; }
			set { _HeaderWin10StyleVM = value; OnPropertyChanged(() => HeaderWin10StyleVM); HeaderWin10StyleVM.BackButtonPressed += HeaderWin10StyleVM_BackButtonPressed; }
		}

		private void HeaderWin10StyleVM_BackButtonPressed()
		{
			switch (Mode)
			{
				case MainWindowMode.BrowseStorage:
					Mode = MainWindowMode.EnterLoginAndPass;
					break;
			}
		}

		public LJ2Book.FormLogin.LoginViewModel LoginVM { get; set; }
		public LJ2Book.FormBrowseStorage.BrowseStorageViewModel BrowseStorageVM { get; set; }
		public LJ2Book.FormBrowseBlog.BrowseBlogViewModel BrowseBlogVM { get; set; }
		private bool RememberLoginAndPass { get => LoginVM.RememberLoginAndPass; }
		private bool _Online = false;
		public bool Online { get => _Online; set => _Online = value; }
		public MainWindowViewModel()
		{
			LoginVM = new FormLogin.LoginViewModel(this);
			BrowseStorageVM = new FormBrowseStorage.BrowseStorageViewModel(this);
			BrowseBlogVM = new FormBrowseBlog.BrowseBlogViewModel(this);

			var context = App.db;
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

		public bool CheckLoginAndPass(string _Login, string _encryptedPass)
		{
			if (Download.DownloadManager.TryLogin(_Login, _encryptedPass))
			{
				this.Online = true;
				this.Mode = MainWindowMode.BrowseStorage;
				OnPropertyChanged(() => Mode);
				OnPropertyChanged(() => Online);
				return true;
			}
			MessageBox.Show(Application.Current.MainWindow, "Invalid login or password");
			return false;
		}
		public Visibility LoginControlVisibility
		{
			get
			{
				return Mode == MainWindowMode.EnterLoginAndPass ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public Visibility BrowseStorageControlVisibility
		{
			get
			{
				return Mode == MainWindowMode.BrowseStorage ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public Visibility ReadBlogControlVisibility
		{
			get
			{
				return Mode == MainWindowMode.ReadBlog ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public MainWindowMode Mode
		{
			get
			{
				return _Mode;
			}
			set
			{
				HeaderWin10StyleVM.BackButtonEnable = !(value == MainWindowMode.EnterLoginAndPass);

				if (_Mode == MainWindowMode.EnterLoginAndPass && this.Online)
					ProcessLoginForm();

				_Mode = value;
				OnPropertyChanged(() => LoginControlVisibility);
				OnPropertyChanged(() => BrowseStorageControlVisibility);
				OnPropertyChanged(() => ReadBlogControlVisibility);

				if (_Mode == MainWindowMode.ReadBlog)
					BrowseBlogVM.RefreshArticlesList();
			}
		}

		private void ProcessLoginForm()
		{
			var context = App.db;
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
		}
	}
}
