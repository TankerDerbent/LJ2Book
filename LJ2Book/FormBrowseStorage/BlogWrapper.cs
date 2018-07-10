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
		public int ProgressValueStage1 { get; set; }
		public int ProgressValueStage2 { get; set; }
		private DateTime _DownloadStarted = DateTime.MinValue;

		private BlogWrapper()
		{
			ProgressMax = 10;
			ProgressValueStage1 = 3;
			ProgressValueStage2 = 3;
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
			dwmgr.ArticlesLoadingOverallProgressChangedStage2 += Dwmgr_ArticlesLoadProgressChanged;
			dwmgr.ArticlesLoadProgressStepStage1 += Dwmgr_ArticlesLoadProgressStepStage1;
			dwmgr.ArticlesLoadProgressStepStage2 += Dwmgr_ArticlesLoadProgressStepStage2;
			dwmgr.Update(blog);
		}

		private void Dwmgr_ArticlesLoadProgressStepStage1()
		{
			ProgressValueStage1 += 1;

			if (ProgressValueStage1 < ProgressMax)
			{
				OnPropertyChanged(() => ProgressValueStage1);
				//OnPropertyChanged(() => RemainedTimeText);
				//OnPropertyChanged(() => ReadyItemsText);
				return;
			}
		}

		public string RemainedTimeText
		{
			get
			{
				if (IsUpdating)
				{
					if (ProgressValueStage2 == 0)
						return string.Empty;
					else
						return TimeSpan.FromTicks((DateTime.Now - _DownloadStarted).Ticks * ProgressMax / ProgressValueStage2).ToString(@"hh\:mm\:ss");
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
					return string.Format("{0} of {1} ready", ProgressValueStage2, ProgressMax);
				}
				else
					return string.Empty;
			}
		}
		private void Dwmgr_ArticlesLoadProgressStepStage2()
		{
			ProgressValueStage2 += 1;
			
			if (ProgressValueStage2 < ProgressMax)
			{
				OnPropertyChanged(() => ProgressValueStage2);
				OnPropertyChanged(() => RemainedTimeText);
				OnPropertyChanged(() => ReadyItemsText);
				return;
			}
			IsUpdating = false;
		}

		private void Dwmgr_ArticlesLoadProgressChanged(int MaxItems)
		{
			ProgressMax = MaxItems;
			ProgressValueStage1 = 0;
			ProgressValueStage2 = 0;
			OnPropertyChanged(() => ProgressMax);
			OnPropertyChanged(() => ProgressValueStage1);
			OnPropertyChanged(() => ProgressValueStage2);
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
