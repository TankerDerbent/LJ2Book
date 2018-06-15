using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;
using System.Windows;
using SimplesNet;
using LJ2Book.DataBase;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace LJ2Book.FormBrowseStorage
{
	class BrowseStorageViewModel : BaseViewModel
	{
		private LJ2Book.MainWindowViewModel RootVM;

		public BrowseStorageViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			RootVM = _RootVM;
			RootVM.db.Blogs.Load();
		}
		public ObservableCollection<Blog> Blogs { get => RootVM.db.Blogs.Local; }
		public ICommand RemoveItem
		{
			get
			{
				return new BaseCommand(x =>
				{
					if (x is Blog)
					{
						Blog blog = x as Blog;
						RootVM.db.Blogs.Remove(blog);
						RootVM.db.SaveChanges();
					}
				});
			}
		}
		public void DoEnter(Window _window)
		{
			if (Blogs.Count >= 1)
				return;

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

					var context = RootVM.db;
					{
						var qryNonBloggers = from u in context.Users select u.UserName;
						sNonBloggers = qryNonBloggers.ToList();
						addForm.DataContext = sNonBloggers;
						addForm.Owner = x as Window;
						
						if (addForm.ShowDialog() ?? false)
						{
							bool bStorePictures = addForm.chkStorePictures.IsChecked ?? false;
							bool bStartImmediatly = addForm.chrStartImmediatly.IsChecked ?? false;

							string sNewBlogName = addForm.ctrlCombo.SelectedItem == null ? sNewBlogName = addForm.ctrlCombo.Text : sNewBlogName = addForm.ctrlCombo.SelectedItem.ToString();
							if (sNewBlogName != addForm.ctrlCombo.Text)
								sNewBlogName = addForm.ctrlCombo.Text;

							User user;
							var qryUser = from u in context.Users where u.UserName.ToLower() == sNewBlogName.ToLower() && u.UserType == UserType.LjUser select u;
							if (qryUser.Count() == 0)
							{
								context.Users.Add(new User { UserName = sNewBlogName, Password = "<empty>", UserType = UserType.LjUser });
								context.SaveChanges();
								qryUser = from u in context.Users where u.UserName.ToLower() == sNewBlogName.ToLower() && u.UserType == UserType.LjUser select u;
							}
							user = qryUser.First();

							user.Blog = new Blog
							{
								KindOfSynchronization = bStartImmediatly ? KindOfSynchronization.Auto : KindOfSynchronization.Manual,
								LastItemNo = -1,
								LastSync = DateTime.MinValue,
								StorePictures = bStorePictures
							};

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
											MessageBox.Show(x as Window, string.Format("Blog '{0}' alreary collected.", sNewBlogName));
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
		}

		internal void RefreshBlogs()
		{
			//RootVM.db.Blogs.Load();
			OnPropertyChanged(() => Blogs);
		}
	}

	class NewBlogItem
	{
		public NewBlogItem()
		{
		}
	}
}
