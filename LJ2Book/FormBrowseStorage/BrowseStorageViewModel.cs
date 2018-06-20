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
			RefreshBlogsView();
			//RootVM.ArticlesLoadProgressChanged += RootVM_ArticlesLoadProgressChanged;
			//RootVM.dwmgr.ArticlesLoadProgressChanged += Dwmgr_ArticlesLoadProgressChanged;
		}
		private void RefreshBlogsView()
		{
			App.db.Blogs.Load();
			_Blogs = (from b in App.db.Blogs.Local select BlogWrapper.FromBlog(b)).ToList();
			OnPropertyChanged(() => Blogs);
		}

		//private void RootVM_ArticlesLoadProgressChanged(Blog blog, int MaxItems, int CurrentItem)
		//{
		//	//throw new NotImplementedException();
		//}

		//public ObservableCollection<Blog> Blogs { get => RootVM.db.Blogs.Local; }
		private List<BlogWrapper> _Blogs;
		public List<BlogWrapper> Blogs { get => _Blogs; internal set { } }
		//{
		//	get
		//	{
		//		List<BlogWrapper> result = new List<BlogWrapper>();
		//		return (from b in RootVM.db.Blogs.Local select BlogWrapper.FromBlog(b)).ToList();
		//	}
		//}

		public ICommand RemoveItem
		{
			get
			{
				return new BaseCommand(x =>
				{
					if (x is BlogWrapper)
					{
						LJ2Book.DataBase.Blog blog = (x as BlogWrapper).blog;
						App.db.Blogs.Remove(blog);
						App.db.SaveChanges();
						RefreshBlogsView();
					}
				});
			}
		}
		public ICommand UpdateItem
		{
			get
			{
				return new BaseCommand(x =>
				{
					if (x is BlogWrapper)
					{
						(x as BlogWrapper).Update();
						//LJ2Book.DataBase.Blog blog = (x as BlogWrapper).blog;
						//RootVM.dwmgr.Update(blog);
					}
				},
				delegate ()
				{
					return RootVM.Online;
				});
			}
		}
		public ICommand ClearArticles
		{
			get
			{
				return new BaseCommand(x =>
				{
					if (x is BlogWrapper)
					{
						LJ2Book.DataBase.Blog blog = (x as BlogWrapper).blog;
						var context = App.db;
						context.Entry(blog).Collection(b => b.Articles).Load();
						context.Articles.RemoveRange(blog.Articles);
						blog.LastItemNo = -1;
						blog.LastSync = DateTime.MinValue;
						context.SaveChanges();
						(x as BlogWrapper).Refresh();
					}
				},
				delegate ()
				{
					return RootVM.Online;
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

					var context = App.db;
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
					RefreshBlogsView();
					//OnPropertyChanged(() => Blogs);
				},
				delegate ()
				{
					return RootVM.Online;
				});
			}
		}
		//public void BlogsCollectionChanged()
		//{
		//	OnPropertyChanged(() => Blogs);
		//}

		public override void Dispose()
		{
		}
	}

	//class NewBlogItem
	//{
	//	public NewBlogItem()
	//	{
	//	}
	//}
}
