using System;
using System.Linq;
using System.Text.RegularExpressions;
using LJ2Book.DataBase;

namespace LJ2Book.FormBrowseBlog
{
	class ArticleWrapper
	{
		// const stuff
		private const string HIDDEN_ARTICLE_TITLE_TEXT = "Hidden article";
		private const string NO_TITLE_ARTICLE_TEXT = "(no title)";
		private static Regex regex = new Regex(@"<[^<]+?>");
		// private variables
		private Article article;
		// public props
		public DateTime DT { get => article.ArticleDT; }
		public string DateTimeText { get => DT.ToShortDateString(); }
		public string RawTitle { get => article.RawTitle; }
		public string RawBody { get => article.RawBody; }
		public bool Hidden { get => article.RawTitle.Length == 0 && article.RawBody.Length == 0; }
		public string Title
		{
			get
			{
				if (article.RawTitle.Length == 0)
				{
					if (article.RawBody.Length == 0)
						return HIDDEN_ARTICLE_TITLE_TEXT;

					return NO_TITLE_ARTICLE_TEXT;
				}

				return RawTitle;
			}
		}
		// ctors
		public ArticleWrapper(Article a)
		{
			article = a;
		}
		// public methods
		public bool HasWords(string[] words)
		{
			string[] TitleWords = RawTitle.ToLower().Split(' ').Distinct().ToArray();
			if (TitleWords.Intersect(words).Any())
				return true;

			string FilteredBody = regex.Replace(RawBody.ToLower(), "");
			if (FilteredBody.Split(' ').Intersect(words).Any())
				return true;

			return false;
		}
		public string[] TagArray { get => article.Tags.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); }
	}
}
