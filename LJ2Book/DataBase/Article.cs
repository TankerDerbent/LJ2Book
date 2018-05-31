using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
	public class Article
	{
		[Key]
		public int AtricleID { get; set; }
		//[ForeignKey("User")]
		//public int UserID { get; set; }
		public string RawText { get; set; }
		public virtual User User { get; set; }
	}
}
