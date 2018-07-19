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
			get => _isReverseSorting;
			set
			{
				_isReverseSorting = value;
				SortContent();
				OnPropertyChanged(() => Articles);
				if (FormMode == EFormMode.Text)
					SwitchToText(Articles);
			}
		}
		public ICommand ClearSearchWords { get => new BaseCommand(() => { TextToSearch = string.Empty; FilterChanged(); }); }
		public string ToggleSortText { get => _isReverseSorting ? "Sort ASC" : "Sort DESC"; }
		private void SortContent()
		{
			Articles = IsReverseSorting ? Articles.OrderByDescending(a => a.DT).ToList() : Articles.OrderBy(a => a.DT).ToList();
			OnPropertyChanged(() => ToggleSortText);
		}
		private void SwitchToText(List<ArticleWrapper> _articles)
		{
			BuildTextAndPreparePictures(_articles);
			var ctrl = (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog;
			foreach (var img in _CachedImages)
				ctrl.browser.RegisterResourceHandler(img.Url, img.imageStream, img.mimeType);
			ctrl.browser.LoadHtml(_TextToShow, true);
			ctrl.browser.Visibility = Visibility.Visible;
			ctrl.listbox.Visibility = Visibility.Hidden;
		}
		private List<CachedImage> _CachedImages;
		private void BuildTextAndPreparePictures(List<ArticleWrapper> _articles)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(@"
<!DOCTYPE html>
<html>
<head>
	<meta charset=""utf-8"">
<style>
body {background-color: #FFFFFF;}
h2   {font-family:calibri,verdana;text-align:center;}
p    {font-family:calibri,verdana;text-align:justify;}
</style>
</head>
<body>
");
			int LabelNo = 1;
			_CachedImages = new List<CachedImage>();
			Regex rxImageTag = new Regex("<img[^<]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Regex rxImageUrl = new Regex("src=\\\"([^\\\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			foreach (var a in _articles)
			{
				sb.Append(string.Format("", LabelNo));
				if (_articles.Count == 1)
					sb.Append(string.Format("<H2>{0}</H2>\r\n", a.Title));
				else
					sb.Append(string.Format("<a name='article_{0}'><H2>{0}. {1}</H2></a>\r\n", LabelNo, a.Title));

				LabelNo += 1;
				sb.Append(a.RawBody);
				sb.Append("\r\n");
				if (a.TagArray.Length == 0)
					sb.Append("<p style=\"text-align:right;\">Tags:&nbsp;<span style='color:Silver'>none</span></p>");
				else
					sb.Append("<p style=\"text-align:right;\">Tags:&nbsp;<strong>" + string.Join("</strong>,<strong>", a.TagArray) + "</strong></p>");

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
			sb.Append("<script type=\"text/javascript\">");
			sb.Append(LiveJournalAPI.JavascriptTexts.ScrollNextA);
			sb.Append(LiveJournalAPI.JavascriptTexts.ScrollPrevA);
			sb.Append("</script>");
			sb.Append("\r\n</body>\r\n</html>");
			_TextToShow = sb.ToString();
		}
		private void SwitchToContent()
		{
			SingleSelection = false;
			var ctrl = (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog;
			ctrl.listbox.Visibility = Visibility.Visible;
			ctrl.browser.Visibility = Visibility.Hidden;
		}
		public enum EFormMode { Content, Text }
		private ArticleWrapper selectedArticle;
		public EFormMode FormMode
		{
			get => _formMode;
			set
			{
				_formMode = value;
				if (_formMode == EFormMode.Content)
					SwitchToContent();
				else
				{
					if (selectedArticle == null)
						SwitchToText(Articles);
					else
						SwitchToText(new List<ArticleWrapper> { selectedArticle });
				}
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
		public Visibility IsNavigationButtonVisible { get => (SingleSelection || _formMode == EFormMode.Content) ? Visibility.Hidden : Visibility.Visible; }
		public string ShowModeText { get => _formMode == EFormMode.Text ? "Back to Content" : "Show Text"; }
		private LJ2Book.MainWindowViewModel RootVM;
		public BrowseBlogViewModel()
		{
			TextToSearch = string.Empty;
			_prevSearchText = string.Empty;
			RootVM = null;
			_RawArticles = null;
		}
		public BrowseBlogViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			_formMode = EFormMode.Content;
			TextToSearch = string.Empty;
			_prevSearchText = string.Empty;
			RootVM = _RootVM;
			ReloadTagList();
		}
		public string[] AllTags { get; set; }
		private void ReloadTagList()
		{
			Blog blog = RootVM.BrowseStorageVM.SelectedItem;
			if (blog == null)
				return;

			string[] rawTags = (from a in App.db.Articles where a.Blog.UserBlogID == blog.UserBlogID select a.Tags).Distinct().ToArray();

			AllTags = string.Join(",", rawTags).
				Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).
				Distinct().
				OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).
				ToArray();
			OnPropertyChanged(() => AllTags);
		}
		private string prevTags = string.Empty;
		private string[] _SelectedTags = new string[] { };
		public string[] SelectedTags { get => _SelectedTags; set { _SelectedTags = value; FilterChanged(); } }
		private List<Article> _RawArticles;
		private bool _doNotShowHiddenArticles = true;
		public List<ArticleWrapper> Articles { get; internal set; }
		public bool DoNotShowHiddenArticles { get => _doNotShowHiddenArticles; set { _doNotShowHiddenArticles = value; FilterChanged(); } }
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
			if (_RawArticles == null)
				return;

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
			if (_SelectedTags.Count() > 0)
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
		private string _TextToShow;
		public ICommand NextArticle { get => new BaseCommand(() => ScrollToNextLabel()); }
		public ICommand PrevArticle { get => new BaseCommand(() => ScrollToPrevArticle()); }
		private async void ScrollToPrevArticle()
		{
			await (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog.browser.EvaluateScriptAsync("ScrollPrevA();");
		}
		private async void ScrollToNextLabel()
		{
			await (Application.Current.MainWindow as MainWindow).ctrlBrowseBlog.browser.EvaluateScriptAsync("ScrollNextA();");
		}
		public ICommand ShowSelectedArticle { get => new BaseCommand(x => _ShowSelectedArticle(x as ArticleWrapper)); }
		private bool SingleSelection = false;
		private void _ShowSelectedArticle(ArticleWrapper articleWrapper)
		{
			SingleSelection = true;
			selectedArticle = articleWrapper;
			FormMode = EFormMode.Text;
			selectedArticle = null;
		}
		public override void Dispose()
		{
		}
	}
	class CachedImage
	{
		public string Url { get; set; }
		public string mimeType { get { string ext = Path.GetExtension(Url); return ResourceHandler.GetMimeType(ext.Length == 0 ? ".jpg" : ext); } }
		public MemoryStream imageStream { get; set; }
	}
}
