using System.Windows;
using System.Windows.Input;
using SimplesNet;

namespace HeaderWpf
{
	class ucHeaderWin10StyleVM : BaseViewModel
	{
		private ucHeaderWin10StyleVM() { }
		public string Title { get => Window.Title; }
		public ucHeaderWin10StyleVM(Window _window) : base(_window)
		{
		}
		public Visibility MaximizeButtonVisibility { get => Window.WindowState == WindowState.Normal ? Visibility.Visible : Visibility.Collapsed; }
		public Visibility RestoreButtonVisibility { get => Window.WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed; }
		public new ICommand Maximize
		{
			get
			{
				return new BaseCommand(x =>
				{
					if (Window.WindowState == WindowState.Normal)
						Window.WindowState = WindowState.Maximized;
					else if (Window.WindowState == WindowState.Maximized)
						Window.WindowState = WindowState.Normal;
					OnPropertyChanged(() => MaximizeButtonVisibility);
					OnPropertyChanged(() => RestoreButtonVisibility);
				});
			}
		}
		private bool _BackButtonEnable = false;
		public bool BackButtonEnable
		{
			set
			{
				if (value == _BackButtonEnable)
					return;

				_BackButtonEnable = value;
				OnPropertyChanged(() => BackButtonVisibility);
			}
		}
		public Visibility BackButtonVisibility {  get => _BackButtonEnable ? Visibility.Visible : Visibility.Collapsed; }
		public ICommand Back
		{
			get
			{
				return new BaseCommand(() =>
				{
					if (BackButtonPressed != null)
						BackButtonPressed();
				}, 
				() => { return _BackButtonEnable; });
			}
		}
		public delegate void OnBackButtonPressed();
		public event OnBackButtonPressed BackButtonPressed;

		public override void Dispose()
		{
			//throw new System.NotImplementedException();
		}
	}
}
