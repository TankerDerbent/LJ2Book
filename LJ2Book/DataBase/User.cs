﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
	public class User
	{
		[Key]
		public int UserID { get; set; }
		[Column("Name", TypeName = "nvarchar")]
		[MaxLength(255)]
		public string UserName { get; set; }
		[Required]
		public int Type { get; set; }
		public string Password { get; set; }
		public virtual ICollection<Article> Articles { get; set; }
		public override string ToString()
		{
			string sLoginInfo = "w/o login";
			if (this.Password.Length > 0)
				sLoginInfo = "pass hash = " + this.Password;

			return string.Format("User {0} '{1}', type {2}, {3}", this.UserID.ToString("D4"), this.UserName, this.Type.ToString(), sLoginInfo);
		}
	}
}
