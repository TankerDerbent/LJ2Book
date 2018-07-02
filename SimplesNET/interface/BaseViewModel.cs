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
			get { return new BaseCommand(x => CloseWindow(x as Window)); }
		}

		public ICommand Maximize
		{
			get { return new BaseCommand(x => MaximizeApplication(x as Window)); }
		}

		public ICommand Minimize
		{
			get { return new BaseCommand(x => MinimizeApplication(x as Window)); }
		}

		protected virtual void CloseWindow(Window window)
		{
			if (window == null)
				window = Window != null ? Window : Application.Current.MainWindow;
			window.Close();
		}

		private void MaximizeApplication(Window window)
		{
			if (window == null)
				window = Window != null ? Window : Application.Current.MainWindow;
			if (window.WindowState == WindowState.Maximized)
				window.WindowState = WindowState.Normal;
			else
				window.WindowState = WindowState.Maximized;
		}

		private void MinimizeApplication(Window window)
		{
			if (window == null)
				window = Window != null ? Window : Application.Current.MainWindow;
			if (window.WindowState == WindowState.Minimized)
				window.WindowState = WindowState.Normal;
			else
				window.WindowState = WindowState.Minimized;
		}

		public abstract void Dispose();
	}
}
