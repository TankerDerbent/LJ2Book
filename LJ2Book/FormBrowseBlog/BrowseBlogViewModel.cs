using System.Linq;
using System.Collections.Generic;
using System.Windows;
using SimplesNet;
using LJ2Book.DataBase;
using System.Diagnostics;

namespace LJ2Book.FormBrowseBlog
{
	class BrowseBlogViewModel : BaseViewModel
	{
		private LJ2Book.MainWindowViewModel RootVM;

		public BrowseBlogViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			RootVM = _RootVM;
			RefreshArticlesList();
		}
		public List<Article> Articles { get; internal set; }
		public void RefreshArticlesList()
		{
			Blog blog = RootVM.BrowseStorageVM.SelectedItem;
			if (blog == null)
			{
				Articles = new List<Article>();
				Debug.WriteLine("RefreshArticlesList: list is empty");
				return;
			}
			Articles = (from a in App.db.Articles where a.Blog.UserBlogID == blog.UserBlogID select a).ToList();
		}

		public override void Dispose()
		{
		}
	}
}
