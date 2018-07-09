using CefSharp;
using SimplesNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace TryShowBlocks
{
	class MainWindowViewModel : Notify
	{
		private enum EFormMode { Content, Text }
		private EFormMode FormMode { get => _formMode; set { _formMode = value; OnPropertyChanged(() => IsNavigationButtonVisible); } }
		public List<ArticleWrapper> Articles { get; internal set; }
		public Visibility IsNavigationButtonVisible { get => FormMode == EFormMode.Content ? Visibility.Hidden : Visibility.Visible; }
		public bool TextShown { get { return FormMode == EFormMode.Text; } set { FormMode = value ? EFormMode.Text : EFormMode.Content; OnPropertyChanged(() => TextShown); } }
		public MainWindowViewModel()
		{
			FormMode = EFormMode.Content;
			Articles = new List<ArticleWrapper>();
			DateTime dtStart = DateTime.Parse("1.1.2018 16:35");
			for (int i = 1; i < 5; i++)
			{
				string[] Lines = File.ReadAllLines(string.Format("C:\\Projects\\WinApps\\LJ2Book\\_Mocks\\TryShowBlocks\\files\\{0}.htm", i), Encoding.UTF8);
				Articles.Add(new ArticleWrapper(dtStart.AddDays(i), Lines[0], string.Join("\r\n", Lines.Skip(1))));
			}
			IsReverseSorting = false;
			SortContent();
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
			await (Application.Current.MainWindow as MainWindow).browser.EvaluateScriptAsync(script);
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
			await (Application.Current.MainWindow as MainWindow).browser.EvaluateScriptAsync(script);
		}
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
		private void SortContent()
		{
			Articles = IsReverseSorting ? Articles.OrderByDescending(a => a.DT).ToList() : Articles.OrderBy(a => a.DT).ToList();
		}

		public ICommand ToggleMode
		{
			get
			{
				return new BaseCommand(() =>
				{
					if (FormMode == EFormMode.Content)
						SwitchToContent();
					else
						SwitchToText();
				});
			}
		}
		private const int animationDuration = 2000;

		private void SwitchToText()
		{
			BuildText();
			(Application.Current.MainWindow as MainWindow).browser.LoadHtml(_TextToShow);
			(Application.Current.MainWindow as MainWindow).browser.Visibility = Visibility.Visible;
			(Application.Current.MainWindow as MainWindow).listbox.Visibility = Visibility.Hidden;
		}
		private void SwitchToContent()
		{
			(Application.Current.MainWindow as MainWindow).listbox.Visibility = Visibility.Visible;
			(Application.Current.MainWindow as MainWindow).browser.Visibility = Visibility.Hidden;
		}
		private string _TextToShow;
		private bool _isReverseSorting;
		private EFormMode _formMode;

		private void BuildText()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<html>\r\n<head>\r\n<meta charset=\"utf-8\">");
			sb.Append("</head>\r\n<body>");
			int LabelNo = 1;
			foreach (var a in Articles)
			{
				sb.Append(string.Format("<a name='article_{0}' />", LabelNo));
				sb.Append(string.Format("<H1>{0}. ", LabelNo));
				LabelNo += 1;
				sb.Append(a.Title);
				sb.Append("</H1>\r\n");
				sb.Append(a.RawBody);
				sb.Append("\r\n");
			}
			sb.Append("\r\n</body>\r\n</html>");
			_TextToShow = sb.ToString();
		}
	}

	public class ArticleWrapper
	{
		// const stuff
		private const string HIDDEN_ARTICLE_TITLE_TEXT = "Hidden article";
		private const string NO_TITLE_ARTICLE_TEXT = "(no title)";
		private static Regex regex = new Regex(@"<[^<]+?>");
		// private variables
		DateTime _DT;
		string _RawTitle;
		string _RawBody;
		// public props
		public DateTime DT { get => _DT; }
		public string DateTimeText { get => DT.ToShortDateString(); }
		public string RawTitle { get => _RawTitle; }
		public string RawBody { get => _RawBody; }
		public bool Hidden { get => _RawTitle.Length == 0 && _RawBody.Length == 0; }
		public string Title
		{
			get
			{
				if (_RawTitle.Length == 0)
				{
					if (_RawBody.Length == 0)
						return HIDDEN_ARTICLE_TITLE_TEXT;

					return NO_TITLE_ARTICLE_TEXT;
				}

				return RawTitle;
			}
		}
		// ctors
		public ArticleWrapper(DateTime _dt, string _title, string _body)
		{
			_DT = _dt;
			_RawTitle = _title;
			_RawBody = _body;
		}
		// public methods
		public bool HasWords(string[] words)
		{
			string[] TitleWords = RawTitle.ToLower().Split(' ');
			if (TitleWords.Intersect(words).Any())
				return true;

			string FilteredBody = regex.Replace(RawBody.ToLower(), "");
			if (FilteredBody.Split(' ').Intersect(words).Any())
				return true;

			return false;
		}
	}
}
