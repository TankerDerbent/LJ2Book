﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
	public enum KindOfSynchronization { Ignore, Manual, Auto }
	public class Blog
	{
		public Blog()
		{
			Articles = new List<Article>();
		}
		[ForeignKey("User"), Key]
		public int UserBlogID { get; set; }
		public KindOfSynchronization KindOfSynchronization { get; set; }
		public int LastItemNo { get; set; }
		public DateTime LastSync { get; set; }
		public bool StorePictures { get; set; }
		public virtual User User { get; set; }
		public ICollection<Article> Articles { get; set; }
		//[NotMapped]
		//public string BlogName { get => User.UserName; }
		//[NotMapped]
		//public string KindOfSynchronizationText { get => KindOfSynchronization == KindOfSynchronization.Auto ? "Auto" : "Manual";  }
		//[NotMapped]
		//public string LastUpdateAsText { get => LastSync == DateTime.MinValue ? "Never" : LastSync.ToString(); }
		//[NotMapped]
		//public string LastItemNoText { get => LastItemNo < 1 ? "Unknown" : LastItemNo.ToString(); }
	}
}
