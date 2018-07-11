using LJ2Book.DataBase;
using SimplesNet;
using System;
using System.Text;
using System.Windows;

namespace LJ2Book.FormBrowseStorage
{
	internal class BlogWrapper : Notify
	{
		public Blog blog { get; internal set; }
		public string BlogNameToShow { get => blog.User.UserName.ToUpper(); }
		public string LastUpdateAndArticlesCountAsText
		{
			get
			{
				StringBuilder result = new StringBuilder();
				result.Append(blog.KindOfSynchronization == KindOfSynchronization.Auto ? "Auto update" : "Manual update");
				result.Append(", ");
				if (blog.LastSync == DateTime.MinValue)
				{
					result.Append("not updated yet");
				}
				else
				{
					result.Append("last update ");
					DateTime now = DateTime.Now;
					if (now.Year == blog.LastSync.Year)
					{
						if (now.Month == blog.LastSync.Month && now.Day == blog.LastSync.Day)
							result.Append(blog.LastSync.ToString("HH:mm"));
						else
							result.Append(blog.LastSync.ToString("m"));
					}
					else
						result.Append(blog.LastSync.ToString("d"));
					result.Append(", ");
					result.Append(blog.LastItemNo);
					result.Append(" articles");
				}

				return result.ToString();
			}
		}
		public bool CanRead { get => blog.LastSync != DateTime.MinValue; }
		public int CurrentStageProgressMax { get; set; }
		public int CurrentStageProgressValue { get; set; }
		private DateTime _DownloadStarted = DateTime.MinValue;

		private BlogWrapper()
		{
			CurrentStageProgressMax = 1;
			CurrentStageProgressValue = 0;
		}
		public static BlogWrapper FromBlog(Blog _blog)
		{
			return new BlogWrapper { blog = _blog };
		}

		public void Update()
		{
			_DownloadStarted = DateTime.Now;
			Download.DownloadManager dwmgr = Download.DownloadManager.Instance;
			var task = new Download.DownloadManager.BlogSynchronizationTask(System.Threading.SynchronizationContext.Current, blog);
			task.BlogSummaryChanged += Dwmgr_BlogInfoArrived;
			task.OverallProgressChangedStage1 += OnOverallProgressChangedStage1;
			task.OverallProgressChangedStage2 += OnOverallProgressChangedStage2;
			task.StepProgressStage1 += OnStepProgressStage1;
			task.StepProgressStage2 += OnStepProgressStage2;
			task.SynchronizationEnded += OnSynchronizationEnded;
			Stage = 1;
			dwmgr.AddSyncTask(task);
		}
		private int _Stage = 0;
		private int Stage {
			get => _Stage;
			set
			{
				_Stage = value;
				OnPropertyChanged(() => DownloadProgressVisibility);
				OnPropertyChanged(() => DownloadStageDesc);
				OnPropertyChanged(() => IsIndeterminate);
				OnPropertyChanged(() => CurrentStageProgressValue);
				OnPropertyChanged(() => CurrentStageProgressMax);
				OnPropertyChanged(() => CurrentStageProgressValue);
			}
		}
		public Visibility DownloadProgressVisibility { get => Stage > 0 ? Visibility.Visible : Visibility.Collapsed; }
		public bool IsIndeterminate { get => Stage < 2; }
		public string DownloadStageDesc
		{
			get
			{
				switch (_Stage)
				{
					case 1: return "Gathering blog general info...";
					case 2: return "Stage 1: listing articles..";
					case 3: return "Stage 1: downloading articles..";
				}
				return string.Empty;
			}
		}
		private void OnSynchronizationEnded()
		{
			Stage = 0;
		}
		private void OnOverallProgressChangedStage1(int MaxItems)
		{
			CurrentStageProgressMax = MaxItems;
			CurrentStageProgressValue = 0;
			Stage = 1;
		}
		private void OnStepProgressStage1()
		{
			CurrentStageProgressValue += 1;
			OnPropertyChanged(() => CurrentStageProgressValue);
		}
		private void OnOverallProgressChangedStage2(int MaxItems)
		{
			CurrentStageProgressMax = MaxItems;
			CurrentStageProgressValue = 0;
			Stage = 2;
		}
		private void OnStepProgressStage2()
		{
			CurrentStageProgressValue += 1;
			OnPropertyChanged(() => CurrentStageProgressValue);
		}

		//public string RemainedTimeText
		//{
		//	get
		//	{
		//		if (IsUpdating)
		//		{
		//			if (ProgressValueStage2 == 0)
		//				return string.Empty;
		//			else
		//				return TimeSpan.FromTicks((DateTime.Now - _DownloadStarted).Ticks * ProgressMax2 / ProgressValueStage2).ToString(@"hh\:mm\:ss");
		//		}
		//		else
		//			return string.Empty;
		//	}
		//}
		//public string ReadyItemsText
		//{
		//	get
		//	{
		//		if (IsUpdating)
		//		{
		//			return string.Format("{0} of {1} ready", ProgressValueStage2, ProgressMax2);
		//		}
		//		else
		//			return string.Empty;
		//	}
		//}

		private void Dwmgr_BlogInfoArrived()
		{
			OnPropertyChanged(() => LastUpdateAndArticlesCountAsText);
		}

		internal void Refresh()
		{
			OnPropertyChanged(() => LastUpdateAndArticlesCountAsText);
		}
	}
}
