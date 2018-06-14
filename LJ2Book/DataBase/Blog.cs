using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
	public enum KindOfSynchronization { Ignore, Manual, Auto }
	public class Blog
	{
		[ForeignKey("User"), Key]
		public int UserBlogID { get; set; }
		public KindOfSynchronization KindOfSynchronization { get; set; }
		public int LastItemNo { get; set; }
		public DateTime LastSync { get; set; }
		public bool StorePictures { get; set; }
		public virtual User User { get; set; }
		[NotMapped]
		public string BlogName { get => User.UserName; }
		[NotMapped]
		public string KindOfSynchronizationText { get => KindOfSynchronization == KindOfSynchronization.Auto ? "Auto" : "Manual";  }
	}
}
