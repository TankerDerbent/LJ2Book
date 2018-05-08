using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace SimplesNet
{
	public static class ScreenExtensions
	{
		public static void GetDpi(this System.Windows.Forms.Screen screen, DpiType dpiType, out uint dpiX, out uint dpiY)
		{
			var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
			var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
			try
			{
				GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
			}
			catch (System.Exception)
			{
				using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
				{
					dpiX = (uint)g.DpiX;
					dpiY = (uint)g.DpiY;
				}
			}
		}

		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromPoint([In]System.Drawing.Point pt, [In]uint dwFlags);

		[DllImport("Shcore.dll")]
		private static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]DpiType dpiType, [Out]out uint dpiX, [Out]out uint dpiY);
	}

	public enum DpiType
	{
		Effective = 0,
		Angular = 1,
		Raw = 2,
	}

	public enum PositionY
	{
		Center,
		Top,
		Bottom
	}

	public enum PositionX
	{
		Center,
		Left,
		Right
	}

	public static class UIHelper
	{
		public static double GetDefaultTopPosition(Screen screen, double height, PositionY pos = PositionY.Center)
		{
			double val = 0;
			if (screen != null && !screen.WorkingArea.IsEmpty)
			{
				uint x, y;
				screen.GetDpi(DpiType.Effective, out x, out y);
				var ps = 96D / (double)x;
				switch(pos)
				{
					case PositionY.Top: val = screen.WorkingArea.Top; break;
					case PositionY.Bottom: val = screen.WorkingArea.Top + ((screen.WorkingArea.Height * ps) - height); break;
					default : val = screen.WorkingArea.Top + ((screen.WorkingArea.Height * ps) / 2D - (height / 2D)); break;
				}
			}
			return val;
		}

		public static double GetDefaultLeftPosition(Screen screen, double width, PositionX pos = PositionX.Center)
		{
			double val = 0;
			if (screen != null && !screen.WorkingArea.IsEmpty)
			{
				uint x, y;
				screen.GetDpi(DpiType.Effective, out x, out y);
				var ps = 96.0 / (double)x;
				switch (pos)
				{
					case PositionX.Left: val = screen.WorkingArea.Left; break;
					case PositionX.Right: val = screen.WorkingArea.Left + ((screen.WorkingArea.Width * ps) - width); break;
					default: val = screen.WorkingArea.Left + ((screen.WorkingArea.Width * ps) / 2D - (width / 2D)); break;
				}
			}
			return val;
		}
	}
}