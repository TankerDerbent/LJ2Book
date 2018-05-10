using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Threading;
using System.Windows.Threading;

namespace SimplesNet
{
	public abstract class BaseViewModel : Notify, IDisposable
	{
		protected SynchronizationContext syncContext { get; set; }

		public Window Window { get; set; }

		public BaseViewModel(Window window = null)
		{
			Window = window;
			if (SynchronizationContext.Current == null || !(SynchronizationContext.Current is DispatcherSynchronizationContext))
			{
				SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
			}
			syncContext = SynchronizationContext.Current;
		}

		public ICommand Close
		{
			get { return new BaseCommand(() => CloseApplication()); }
		}

		public ICommand Maximize
		{
			get { return new BaseCommand(() => MaximizeApplication()); }
		}

		public ICommand Minimize
		{
			get { return new BaseCommand(() => MinimizeApplication()); }
		}

		public ICommand DragMove
		{
			get { return new BaseCommand(() => DragMoveCommand()); }
		}



		private void DragMoveCommand()
		{
			Window.DragMove();
		}

		protected virtual void CloseApplication()
		{
			Application.Current.Shutdown();
		}

		private void MaximizeApplication()
		{
			if (Window.WindowState == WindowState.Maximized)
				Window.WindowState = WindowState.Normal;
			else
				Window.WindowState = WindowState.Maximized;
		}

		private void MinimizeApplication()
		{
			if (Window.WindowState == WindowState.Minimized)
			{
				Window.Opacity = 1;
				Window.WindowState = WindowState.Normal;
			}
			else
			{
				Window.Opacity = 0;
				Window.WindowState = WindowState.Minimized;
			}
		}

		public abstract void Dispose();
	}
}
