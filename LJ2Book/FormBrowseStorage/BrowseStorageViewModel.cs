using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using SimplesNet;
using LJ2Book.DataBase;
using System.Windows.Input;

namespace LJ2Book.FormBrowseStorage
{
	class BrowseStorageViewModel : BaseViewModel
	{
		private LJ2Book.MainWindowViewModel RootVM;
		private List<object> _Blogs;

		public BrowseStorageViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			RootVM = _RootVM;
			ReloadBlogList();
		}
		private void ReloadBlogList()
		{
			List<object> blogs = new List<object>();
			using (var context = new SiteContext("DefaultConnection"))
			{
				var qryBlogs = from u in context.Users where u.UserBlog == UserBlog.Reload || u.UserBlog == UserBlog.Store select u;
				foreach (var u in qryBlogs)
				{
					blogs.Add(new Blog { Name = u.UserName, LastSyncDateTime = DateTime.MinValue, IsSynchronizing = (u.UserBlog == UserBlog.Reload), SynchronizingProgress = 0 });
				}
				blogs.Add(new NewBlogItem(this));
			}
			Blogs = blogs;
		}
		public List<object> Blogs
		{
			get => _Blogs;
			set
			{
				_Blogs = value;
				OnPropertyChanged(() => Blogs);
			}
		}
		public ICommand RemoveItem
		{
			get
			{
				return new BaseCommand(x =>
				{
					string s = string.Format("called for item '{0}'", (x as Blog).Name);
					MessageBox.Show(s);
				});
			}
		}
		//BaseCommand
		public void DoEnter(Window _window)
		{
			if (Blogs.Count != 1)
				return;

			//(Blogs.FirstOrDefault() as NewBlogItem).
			NewBlogCommand.Execute(_window);
		}
		public ICommand NewBlogCommand
		{
			get
			{
				return new BaseCommand(x =>
				{
					List<string> sNonBloggers = new List<string>();
					LJ2Book.SimpleForms.AddBlog addForm = new SimpleForms.AddBlog();

					using (var context = new SiteContext())
					{
						var qryNonBloggers = from u in context.Users where u.UserBlog == UserBlog.Ignore select u.UserName;
						sNonBloggers = qryNonBloggers.ToList();
						addForm.DataContext = sNonBloggers;
						addForm.Owner = x as Window;
						string sNewBlogger;
						if (addForm.ShowDialog() ?? false)
						{
							if (addForm.ctrlCombo.SelectedItem == null)
							{
								sNewBlogger = addForm.ctrlCombo.Text;
								context.Users.Add(new User { UserName = addForm.ctrlCombo.Text, Password = "*", UserBlog = UserBlog.Reload, UserType = UserType.LjUser });
							}
							else
							{
								sNewBlogger = addForm.ctrlCombo.SelectedItem.ToString();
								var qryUser = from u in context.Users where u.UserName == sNewBlogger select u;
								User user = qryUser.First();
								user.UserBlog = UserBlog.Reload;
								context.Entry(user).State = System.Data.Entity.EntityState.Modified;
							}
							try
							{
								context.SaveChanges();
							}
							catch (System.Data.Entity.Infrastructure.DbUpdateException e)
							{
								if (e.InnerException is System.Data.Entity.Core.UpdateException)
								{
									System.Data.Entity.Core.UpdateException e2 = (e.InnerException as System.Data.Entity.Core.UpdateException);
									if (e2.InnerException is System.Data.SQLite.SQLiteException)
									{
										System.Data.SQLite.SQLiteException e3 = e2.InnerException as System.Data.SQLite.SQLiteException;
										if (e3.ResultCode == System.Data.SQLite.SQLiteErrorCode.Constraint)
										{
											MessageBox.Show(x as Window, string.Format("Blog '{0}' alreary collected.", sNewBlogger));
										}
									}
								}
								else
									throw e;
							}
						}
					}
					RefreshBlogs();
				});
			}
		}

		public override void Dispose()
		{
			//throw new System.NotImplementedException();
		}

		internal void RefreshBlogs()
		{
			ReloadBlogList();
			//throw new NotImplementedException();
			OnPropertyChanged(() => Blogs);
		}
	}

	class Blog
	{
		public string Name { get; set; }
		public DateTime LastSyncDateTime { get; set; }
		public bool IsSynchronizing { get; set; }
		public int SynchronizingProgress { get; set; }
		//public bool NewItem { get; set; }
	}
	class NewBlogItem
	{
		private BrowseStorageViewModel vm;
		public NewBlogItem(BrowseStorageViewModel _vm)
		{
			this.vm = _vm;
		}
		//public string Name { get; set; }
	}
}
