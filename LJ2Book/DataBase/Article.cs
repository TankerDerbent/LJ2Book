﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
	public enum ArticleState { Unknown, Queued, Ready, FailedToProcess }
	public class Article
	{
		[Key]
		public int AtricleID { get; set; }
		[Index("IX_UniqueArticle", IsUnique = true)]
		public int AtricleNo { get; set; }
		public int Anum { get; set; }
		public ArticleState State { get; set; }
		public string Url { get; set; }
		public DateTime ArticleDT { get; set; }
		public string RawTitle { get; set; }
		public string RawBody { get; set; }
		[Index("IX_UniqueArticle", IsUnique = true)]
		public virtual Blog Blog { get; set; }
	}
}
