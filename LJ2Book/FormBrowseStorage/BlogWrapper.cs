using LJ2Book.DataBase;
using SimplesNet;
using System;
using System.Diagnostics;
using System.Linq;
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
					App.db.Entry(blog).Collection(b => b.Articles).Load();
					int articlesCount = (from a in blog.Articles where a.State == ArticleState.Queued select a).Count();
					if (articlesCount == 0)
						result.Append('.');
					else
						result.Append(string.Format(", {0} to load.", articlesCount));
				}

				return result.ToString();
			}
		}
		public bool CanRead { get => (blog.LastSync != DateTime.MinValue) && (_Stage == 0); }
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
				OnPropertyChanged(() => CanRead);
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
					case 1: return "Stage 1 of 3: gathering general info...";
					case 2: return "Stage 2 of 3: listing articles..";
					case 3: return "Stage 3 of 3: downloading articles..";
				}
				return string.Empty;
			}
		}
		private void OnSynchronizationEnded()
		{
			Stage = 0;
			OnPropertyChanged(() => LastUpdateAndArticlesCountAsText);
		}
		private void OnOverallProgressChangedStage1(int MaxItems)
		{
			CurrentStageProgressMax = MaxItems;
			CurrentStageProgressValue = 0;
			Stage = 2;
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
			Stage = 3;
		}
		private void OnStepProgressStage2()
		{
			CurrentStageProgressValue += 1;
			OnPropertyChanged(() => CurrentStageProgressValue);
		}
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
