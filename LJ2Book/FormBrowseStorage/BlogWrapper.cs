using LJ2Book.DataBase;
using SimplesNet;
using System;
using System.Windows;

namespace LJ2Book.FormBrowseStorage
{
	internal class BlogWrapper : Notify
	{
		public Blog blog { get; internal set; }
		public string BlogNameToShow { get => blog.User.UserName.ToUpper(); }
		public string KindOfSynchronizationText { get => blog.KindOfSynchronization == KindOfSynchronization.Auto ? "Auto" : "Manual"; }
		public string LastUpdateAsText { get => blog.LastSync == DateTime.MinValue ? "Never" : blog.LastSync.ToString(); }
		public string LastItemNoText { get => blog.LastItemNo < 1 ? "Unknown" : blog.LastItemNo.ToString(); }
		private bool _IsUpdating = false;
		public bool IsUpdating
		{
			get
			{
				return _IsUpdating;
			}
			set
			{
				_IsUpdating = value;
				OnPropertyChanged(() => BlogStatusVisibility);
				OnPropertyChanged(() => UpdateProgressVisibility);
			}
		}
		public Visibility BlogStatusVisibility { get { return IsUpdating ? Visibility.Collapsed : Visibility.Visible; } }
		public Visibility UpdateProgressVisibility { get { return IsUpdating ? Visibility.Visible : Visibility.Collapsed; } }
		public bool CanRead { get => blog.LastSync != DateTime.MinValue; }
		public int ProgressMax { get; set; }
		public int ProgressValue { get; set; }
		private DateTime _DownloadStarted = DateTime.MinValue;

		private BlogWrapper()
		{
			ProgressMax = 10;
			ProgressValue = 3;
		}
		public static BlogWrapper FromBlog(Blog _blog)
		{
			return new BlogWrapper { blog = _blog };
		}

		public void Update()
		{
			_DownloadStarted = DateTime.Now;
			Download.DownloadManager dwmgr = new Download.DownloadManager(System.Threading.SynchronizationContext.Current);
			dwmgr.BlogInfoArrived += Dwmgr_BlogInfoArrived;
			dwmgr.ArticlesLoadProgressChanged += Dwmgr_ArticlesLoadProgressChanged;
			dwmgr.ArticlesLoadProgressStep += Dwmgr_ArticlesLoadProgressStep;
			dwmgr.Update(blog);
		}
		public string RemainedTimeText
		{
			get
			{
				if (IsUpdating)
				{
					if (ProgressValue == 0)
						return string.Empty;
					else
						return TimeSpan.FromTicks((DateTime.Now - _DownloadStarted).Ticks * ProgressMax / ProgressValue).ToString(@"hh\:mm\:ss");
				}
				else
					return string.Empty;
			}
		}
		public string ReadyItemsText
		{
			get
			{
				if (IsUpdating)
				{
					return string.Format("{0} of {1} ready", ProgressValue, ProgressMax);
				}
				else
					return string.Empty;
			}
		}
		private void Dwmgr_ArticlesLoadProgressStep()
		{
			ProgressValue += 1;
			//TimeSpan ts = DateTime.Now - _DownloadStarted;
			//TimeSpan remainedTs = TimeSpan.FromTicks(ts.Ticks * ProgressMax / ProgressValue);
			
			if (ProgressValue < ProgressMax)
			{
				OnPropertyChanged(() => ProgressValue);
				OnPropertyChanged(() => RemainedTimeText);
				OnPropertyChanged(() => ReadyItemsText);
				return;
			}
			IsUpdating = false;
		}

		private void Dwmgr_ArticlesLoadProgressChanged(int MaxItems)
		{
			ProgressMax = MaxItems;
			ProgressValue = 0;
			OnPropertyChanged(() => ProgressMax);
			OnPropertyChanged(() => ProgressValue);
			OnPropertyChanged(() => RemainedTimeText);
			OnPropertyChanged(() => ReadyItemsText);
			IsUpdating = true;
		}

		private void Dwmgr_BlogInfoArrived(int lastItemNo, DateTime dateLastSync)
		{
			OnPropertyChanged(() => LastItemNoText);
			OnPropertyChanged(() => LastUpdateAsText);
		}

		internal void Refresh()
		{
			OnPropertyChanged(() => LastItemNoText);
			OnPropertyChanged(() => LastUpdateAsText);
			OnPropertyChanged(() => BlogStatusVisibility);
			OnPropertyChanged(() => UpdateProgressVisibility);
		}
	}
}
