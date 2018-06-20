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
				OnPropertyChanged(() => UpdatePrograssVisibility);
			}
		}
		public Visibility BlogStatusVisibility { get { return IsUpdating ? Visibility.Collapsed : Visibility.Visible; } }
		public Visibility UpdatePrograssVisibility { get { return IsUpdating ? Visibility.Visible : Visibility.Collapsed; } }
		public int ProgressMax { get; set; }
		public int ProgressValue { get; set; }

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
			Download.DownloadManager dwmgr = new Download.DownloadManager(System.Threading.SynchronizationContext.Current);
			dwmgr.BlogInfoArrived += Dwmgr_BlogInfoArrived;
			dwmgr.ArticlesLoadProgressChanged += Dwmgr_ArticlesLoadProgressChanged;
			dwmgr.ArticlesLoadProgressStep += Dwmgr_ArticlesLoadProgressStep;
			dwmgr.Update(blog);
		}

		private void Dwmgr_ArticlesLoadProgressStep()
		{
			ProgressValue += 1;
			
			if (ProgressValue < ProgressMax)
			{
				OnPropertyChanged(() => ProgressValue);
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
			OnPropertyChanged(() => UpdatePrograssVisibility);
		}
	}
}
