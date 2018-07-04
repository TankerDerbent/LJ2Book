using System.Linq;
using System.Collections.Generic;
using System.Windows;
using SimplesNet;
using LJ2Book.DataBase;
using System.Diagnostics;
using System.Windows.Input;
using System;
using System.Collections.ObjectModel;

namespace LJ2Book.FormBrowseBlog
{
	class BrowseBlogViewModel : BaseViewModel
	{
		private LJ2Book.MainWindowViewModel RootVM;
		public BrowseBlogViewModel()
		{
			TextToSearch = string.Empty;
			_prevSearchText = string.Empty;
			RootVM = null;
			_RawArticles = null;
			//Articles = new List<ArticleWrapper>();
			//Articles.Add(new ArticleWrapper(new Article { AtricleNo = 1, ArticleDT = DateTime.Parse("28.05.2018"), RawTitle = "Article 1", RawBody = "text" }));
			//Articles.Add(new ArticleWrapper(new Article { AtricleNo = 2, ArticleDT = DateTime.Parse("29.05.2018"), RawTitle = "Article 2", RawBody = "text" }));
		}
		public BrowseBlogViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			TextToSearch = string.Empty;
			_prevSearchText = string.Empty;
			RootVM = _RootVM;
			_TagsList = new ObservableCollection<TagItem>();
			_TagsList.Add(new TagItem { Name = "tag 1" });
			_TagsList.Add(new TagItem { Name = "tag 2" });
			_TagsList.Add(new TagItem { Name = "tag 3" });
			_TagsList.Add(new TagItem { Name = "tag 4" });
		}
		private List<Article> _RawArticles;
		private bool _doNotShowHiddenArticles;

		public List<ArticleWrapper> Articles { get; internal set; }
		public bool DoNotShowHiddenArticles { get { return _doNotShowHiddenArticles; } set { _doNotShowHiddenArticles = value; FilterChanged(); } }
		public string TextToSearch { get; set; }
		public void RawArticlesListChanged()
		{
			Blog blog = RootVM.BrowseStorageVM.SelectedItem;
			if (blog == null)
			{
				_RawArticles = new List<Article>();
				return;
			}
			_RawArticles = (from a in App.db.Articles where a.Blog.UserBlogID == blog.UserBlogID select a).ToList();

			FilterChanged();
		}
		private string _prevSearchText;
		private void FilterChanged()
		{
			List<ArticleWrapper> Stub = _RawArticles.Select(a => new ArticleWrapper(a)).OrderByDescending(biw => biw.DT).ToList();
			// Hide private Check
			if (_doNotShowHiddenArticles)
				Stub = Stub.Where(a => (!a.Hidden)).ToList();
			// search by text
			if (TextToSearch.Length > 0)
			{
				string[] words = TextToSearch.ToLower().Split(' ');
				Stub = Stub.Where(a => a.HasWords(words)).ToList();

				_prevSearchText = TextToSearch;
			}
			else
			{
				if (_prevSearchText.Length > 0)
					_prevSearchText = string.Empty;
			}
			// end filter parse
			Articles = Stub;

			OnPropertyChanged(() => Articles);
		}
		public ICommand DoSearch
		{
			get
			{
				return new BaseCommand(() => FilterChanged());
			}
		}
		private List<string> _SelectedTags = new List<string>();
		public string SelectedTags
		{
			get
			{
				if (_TagsList == null)
					return string.Empty;

				return _TagsList.Count == 0 ? "select tags..." : string.Join("; ", _SelectedTags.ToArray());
			}
		}
		private ObservableCollection<TagItem> _TagsList;
		public ObservableCollection<TagItem> TagsList { get { return _TagsList; } set { _TagsList = value; OnPropertyChanged(() => TagsList); } }
		public ICommand TagsListChanged
		{
			get
			{
				return new BaseCommand(x =>
				{
					TagItem item = x as TagItem;
					if (item.IsChecked)
						_SelectedTags.Add(item.Name);
					else
					{
						_SelectedTags.Remove(item.Name);
					}
					OnPropertyChanged(() => SelectedTags);
				});
			}
		}
		public override void Dispose()
		{
		}
	}

	class TagItem : Notify
	{
		public string Name { get; set; }
		private bool _IsChecked = false;
		public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; OnPropertyChanged(() => IsChecked); } }
	}
}
