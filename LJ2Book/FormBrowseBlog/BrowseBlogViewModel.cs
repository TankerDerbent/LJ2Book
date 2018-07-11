using System.Linq;
using System.Collections.Generic;
using System.Windows;
using SimplesNet;
using LJ2Book.DataBase;
using System.Diagnostics;
using System.Windows.Input;
using System;
using System.Collections.ObjectModel;
using CefSharp;
using System.Text;
using System.Text.RegularExpressions;
using System.Data.Entity;
using System.IO;

namespace LJ2Book.FormBrowseBlog
{
	class BrowseBlogViewModel : BaseViewModel
	{
		private bool _isReverseSorting = false;
		private EFormMode _formMode;
		public bool IsReverseSorting
		{
			get => _isReverseSorting; set
			{
				_isReverseSorting = value;
				SortContent();
				OnPropertyChanged(() => Articles);
				if (FormMode == EFormMode.Text)
					SwitchToText();
			}
		}
		public string ToggleSortText { get => _isReverseSorting ? "Sort ASC" : "Sort DESC"; }
		private void SortContent()
		{
			Articles = IsReverseSorting ? Articles.OrderByDescending(a => a.DT).ToList() : Articles.OrderBy(a => a.DT).ToList();
			OnPropertyChanged(() => ToggleSortText);
		}
		private void SwitchToText()
		{
			BuildTextAndPreparePictures();
			var ctrl = (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog;
			foreach (var img in _CachedImages)
				ctrl.browser.RegisterResourceHandler(img.Url, img.imageStream, img.mimeType);
			ctrl.browser.LoadHtml(_TextToShow, true);
			ctrl.browser.Visibility = Visibility.Visible;
			ctrl.listbox.Visibility = Visibility.Hidden;
		}
		private List<CachedImage> _CachedImages;
		private void BuildTextAndPreparePictures()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<!DOCTYPE html>\r\n<html>\r\n<head>\r\n<meta charset=\"utf-8\">");
			sb.Append("</head>\r\n<body background='#FFFFFF'>");
			int LabelNo = 1;
			//List<string> imagesUrls = new List<string>();
			_CachedImages = new List<CachedImage>();
			Regex rxImageTag = new Regex("<img[^<]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Regex rxImageUrl = new Regex("src=\\\"([^\\\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			foreach (var a in Articles)
			{
				sb.Append(string.Format("<a name='article_{0}' />", LabelNo));
				sb.Append(string.Format("<H1>{0}. ", LabelNo));
				LabelNo += 1;
				sb.Append(a.Title);
				sb.Append("</H1>\r\n");
				sb.Append(a.RawBody);
				sb.Append("\r\n");

				MatchCollection matches = rxImageTag.Matches(a.RawBody);
				foreach (Match match in matches)
				{
					Match matchUrl = rxImageUrl.Match(match.Groups[0].Value.ToString());
					string url = matchUrl.Groups[1].Value.ToString();
					var picture = App.db.Pictures.Find(new string[] { url });
					if (picture != null)
					{
						Debug.WriteLine(string.Format("Found cached img: '{0}'", url));
						MemoryStream msImage = new MemoryStream();
						msImage.Write(picture.Data, 0, picture.Data.Length);
						_CachedImages.Add(new CachedImage { Url = url, imageStream = msImage });
					}
					else
					{
						Debug.WriteLine(string.Format("Cached img: '{0}' not found", url));
					}
				}
			}
			sb.Append("<script type=\"text/javascript\">var ljsales = document.getElementsByClassName('ljsale'); for (var i = 0; i < ljsales.length; i++) ljsales[i].parentNode.removeChild(ljsales[i]);</script>");
			sb.Append("\r\n</body>\r\n</html>");
			_TextToShow = sb.ToString();
		}
		private void SwitchToContent()
		{
			var ctrl = (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog;
			ctrl.listbox.Visibility = Visibility.Visible;
			ctrl.browser.Visibility = Visibility.Hidden;
		}
		public enum EFormMode { Content, Text }
		public EFormMode FormMode
		{
			get => _formMode;
			set
			{
				_formMode = value;
				if (_formMode == EFormMode.Content)
					SwitchToContent();
				else
					SwitchToText();
				OnPropertyChanged(() => ShowModeText);
				OnPropertyChanged(() => TextShown);
				OnPropertyChanged(() => IsNavigationButtonVisible);
			}
		}
		public bool TextShown
		{
			get => FormMode == EFormMode.Text;
			set
			{
				FormMode = (value ? EFormMode.Text : EFormMode.Content);
				OnPropertyChanged(() => ShowModeText);
				OnPropertyChanged(() => TextShown);
				OnPropertyChanged(() => IsNavigationButtonVisible);
			}
		}
		public Visibility IsNavigationButtonVisible { get => _formMode == EFormMode.Text ? Visibility.Visible : Visibility.Hidden; }
		public string ShowModeText { get => _formMode == EFormMode.Text ? "Basck to Content" : "Show Text"; }
		private LJ2Book.MainWindowViewModel RootVM;
		public BrowseBlogViewModel()
		{
			TextToSearch = string.Empty;
			_prevSearchText = string.Empty;
			RootVM = null;
			_RawArticles = null;
			_TagsList = new ObservableCollection<TagItem>();
			_TagsList.Add(new TagItem { Name = "tag 1" });
			_TagsList.Add(new TagItem { Name = "tag 2" });
			_TagsList.Add(new TagItem { Name = "tag 3" });
			_TagsList.Add(new TagItem { Name = "tag 4" });
		}
		public BrowseBlogViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			_formMode = EFormMode.Content;
			TextToSearch = string.Empty;
			_prevSearchText = string.Empty;
			RootVM = _RootVM;
			ReloadTagList();
		}
		private void ReloadTagList()
		{
			Blog blog = RootVM.BrowseStorageVM.SelectedItem;
			if (blog == null)
				return;

			string[] rawTags = (from a in App.db.Articles where a.Blog.UserBlogID == blog.UserBlogID select a.Tags).Distinct().ToArray();

			string[] resultTags = string.Join(",", rawTags).Split(',').Distinct().OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToArray();
			var Tags = new ObservableCollection<TagItem>();
			if (resultTags.Count() > 0)
			{
				while (resultTags[0].Length == 0)
					resultTags = resultTags.Skip(1).ToArray();

				foreach (var s in resultTags)
					Tags.Add(new TagItem { Name = s });
			}
			TagsList = Tags;
		}
		private string prevTags = string.Empty;
		internal void ApplyTags()
		{
			string currentSelection = string.Join(",", _SelectedTags.ToArray());
			if (prevTags == currentSelection)
				return;

			prevTags = currentSelection;
			FilterChanged();
		}
		private List<Article> _RawArticles;
		private bool _doNotShowHiddenArticles = true;
		public List<ArticleWrapper> Articles { get; internal set; }
		public bool DoNotShowHiddenArticles { get { return _doNotShowHiddenArticles; } set { _doNotShowHiddenArticles = value; FilterChanged(); } }
		public string TextToSearch { get; set; }
		public void SelectedBlogChanged()
		{
			ReloadTagList();
			RawArticlesListChanged();
		}
		private void RawArticlesListChanged()
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
			// search by tags
			if (_SelectedTags.Count > 0)
				Stub = Stub.Where(a => a.TagArray.Intersect(_SelectedTags.ToArray()).Any()).ToList();
			// end filter parse
			Articles = Stub;
			SortContent();
			
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
		private string _TextToShow;
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
		public ICommand NextArticle { get { return new BaseCommand(() => ScrollToNextLabel()); } }
		public ICommand PrevArticle { get { return new BaseCommand(() => ScrollToPrevArticle()); } }
		private async void ScrollToPrevArticle()
		{
			string script = @"
var nPageYOffset = window.pageYOffset;
var labels = document.getElementsByTagName('a');
var len = labels.length;
for(var i = len - 1; i >= 0; i--)
{
	var labelOffset = labels[i].offsetTop;
	if (labelOffset < nPageYOffset)
	{
		window.scrollTo(0, labelOffset);
		break;
	}
}
";
			await (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog.browser.EvaluateScriptAsync(script);
		}

		private async void ScrollToNextLabel()
		{
			string script = @"
var nPageYOffset = window.pageYOffset;
var labels = document.getElementsByTagName('a');
var len = labels.length;
for(var i = 1; i < len; i++)
{
	var labelOffset = labels[i].offsetTop;
	if (labelOffset > nPageYOffset)
	{
		window.scrollTo(0, labelOffset);
		break;
	}
}
";
			await (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog.browser.EvaluateScriptAsync(script);
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
	class CachedImage
	{
		public string Url { get; set; }
		public string mimeType { get { string ext = Path.GetExtension(Url); return ResourceHandler.GetMimeType(ext.Length == 0 ? ".jpg" : ext); } }
		public MemoryStream imageStream { get; set; }
	}
}
