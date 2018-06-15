using System;
using System.ComponentModel.DataAnnotations;

namespace LJ2Book.DataBase
{
	public enum ArticleState { Unknown, Queued, Ready }
	public class Article
	{
		[Key]
		public int AtricleNo { get; set; }
		public int Anum { get; set; }
		public ArticleState State { get; set; }
		public string Url { get; set; }
		public DateTime ArticleDT { get; set; }
		public string RawTitle { get; set; }
		public string RawBody { get; set; }
		[Key]
		public virtual Blog Blog { get; set; }
	}
}
