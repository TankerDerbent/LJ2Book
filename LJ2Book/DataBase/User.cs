﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
	public enum UserType { LjUser, NonLjUser }
	
	public class User
	{
		[Key]
		public int UserID { get; set; }
		[Column("Name", TypeName = "nvarchar")]
		[MaxLength(255)]
		[Index(IsUnique = true)]
		public string UserName { get; set; }
		[Required]
		public string Password { get; set; }
		public UserType UserType { get; set; }
		public virtual Blog Blog {get; set;}
		public override string ToString()
		{
			string sLoginInfo = "w/o login";
			if (this.Password.Length > 0)
				sLoginInfo = "pass hash = " + this.Password;

			return string.Format("User {0} '{1}', type {2}, {3}", this.UserID.ToString("D4"), this.UserName, this.UserType.ToString(), sLoginInfo);
		}
	}
}
