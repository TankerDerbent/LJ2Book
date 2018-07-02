using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Interop;
using System.IO;

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
				if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SHCore.dll")))
				{
					GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
				}
				else
				{
					using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
					{
						dpiX = (uint)g.DpiX;
						dpiY = (uint)g.DpiY;
					}
				}
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

		public static Rectangle GetWorkingAreaDPI(this System.Windows.Forms.Screen screen, DpiType dpiType)
		{
			uint dpiX, dpiY =0;
			GetDpi(screen, dpiType, out dpiX, out dpiY);
			var psX = 96D / (double)dpiX;
			var psY = 96D / (double)dpiY;
			Rectangle rect = new Rectangle(screen.WorkingArea.X, screen.WorkingArea.Y, (int)((double)screen.WorkingArea.Width * psX), (int)((double)screen.WorkingArea.Height * psY));
			return rect;
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

	public enum ShowWindowState : int
	{
		Hide = 0,
		Normal = 1,
		Minimized = 2,
		Maximized = 3,
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct WINDOWPLACEMENT
	{
		public int length;
		public int flags;
		public ShowWindowState showWindowState;
		public System.Drawing.Point ptMinPosition;
		public System.Drawing.Point ptMaxPosition;
		public System.Drawing.Rectangle rcNormalPosition;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left, Top, Right, Bottom;

		public RECT(int left, int top, int right, int bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public int Height
		{
			get { return Bottom - Top; }
		}

		public int Width
		{
			get { return Right - Left; }
		}
	}

	public static class UIHelper
	{
		public static Rectangle[] WorkingAreaDPIOfScreens
		{
			get
			{
				Rectangle[] rects = new Rectangle[System.Windows.Forms.Screen.AllScreens.Length];
				for(int i= 0; i < System.Windows.Forms.Screen.AllScreens.Length; i++)
				{
					rects[i] = ScreenExtensions.GetWorkingAreaDPI(System.Windows.Forms.Screen.AllScreens[i], DpiType.Effective);
				}
				return rects;
			}
		}

		public static double GetDefaultTopPosition(Screen screen, double height, PositionY pos = PositionY.Center)
		{
			double val = 0;
			if (screen != null && !screen.WorkingArea.IsEmpty)
			{
				uint x, y;
				ScreenExtensions.GetDpi(screen, DpiType.Effective, out x, out y);
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
				ScreenExtensions.GetDpi(screen, DpiType.Effective, out x, out y);
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

		public static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
		{
			WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
			placement.length = Marshal.SizeOf(placement);
			GetWindowPlacement(hwnd, ref placement);
			return placement;
		}

		public static bool GetWindowPosLegacy(IntPtr hwnd, out RECT rectLegacy)
		{
			try
			{
				return GetWindowRect(hwnd, out rectLegacy);
			}
			catch (System.Exception)
			{
				rectLegacy = new RECT();
				return false;
			}
		}

		public static void SetWindowOwner(Window wnd, object owner)
		{
			if (wnd != null)
			{
				wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				if (owner != null)
				{
					if (owner is Window)
					{
						wnd.Owner = owner as Window;
						wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					}
					else if (owner is UIElement)
					{
						var detectWnd = Window.GetWindow(owner as UIElement);
						if (detectWnd != null)
						{
							wnd.Owner = detectWnd;
							wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
							return;
						}
						var pSource = PresentationSource.FromVisual(owner as UIElement);
						if (pSource as HwndSource != null)
						{
							var root = GetAncestor((pSource as HwndSource).Handle, GetAncestorFlags.GetRoot);
							WindowInteropHelper wih = new WindowInteropHelper(wnd);
							wih.Owner = root != IntPtr.Zero ? root : (pSource as HwndSource).Handle;
							wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
						}
					}
					else if (owner is IntPtr)
					{
						var root = GetAncestor((IntPtr)owner, GetAncestorFlags.GetRoot);
						WindowInteropHelper wih = new WindowInteropHelper(wnd);
						wih.Owner = root != IntPtr.Zero ? root : (IntPtr)owner;
						wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					}
					else if (owner is long)
					{
						var root = GetAncestor(new IntPtr((long)owner), GetAncestorFlags.GetRoot);
						WindowInteropHelper wih = new WindowInteropHelper(wnd);
						wih.Owner = root != IntPtr.Zero ? root : new IntPtr((long)owner);
						wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					}
					else if (owner is int)
					{
						var root = GetAncestor(new IntPtr((int)owner), GetAncestorFlags.GetRoot);
						WindowInteropHelper wih = new WindowInteropHelper(wnd);
						wih.Owner = root != IntPtr.Zero ? root : new IntPtr((int)owner);
						wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					}
				}
				else if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null)
				{
					wnd.Owner = System.Windows.Application.Current.MainWindow;
					wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}
			}
		}

		#region Import Functions

		enum GetAncestorFlags
		{
			/// <summary>
			/// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
			/// </summary>
			GetParent = 1,
			/// <summary>
			/// Retrieves the root window by walking the chain of parent windows.
			/// </summary>
			GetRoot = 2,
			/// <summary>
			/// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
			/// </summary>
			GetRootOwner = 3
		}

		[DllImport("user32.dll", ExactSpelling = true)]
		static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

		[DllImport("user32.dll")]
		public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		[DllImport("SHCore.dll", SetLastError = true)]
		public static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);

		[DllImport("SHCore.dll", SetLastError = true)]
		public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

		[Flags]
		public enum SetWindowPosFlags
		{
			NOSIZE = 0x0001,
			NOMOVE = 0x0002,
			NOZORDER = 0x0004,
			NOREDRAW = 0x0008,
			NOACTIVATE = 0x0010,
			DRAWFRAME = 0x0020,
			FRAMECHANGED = 0x0020,
			SHOWWINDOW = 0x0040,
			HIDEWINDOW = 0x0080,
			NOCOPYBITS = 0x0100,
			NOOWNERZORDER = 0x0200,
			NOREPOSITION = 0x0200,
			NOSENDCHANGING = 0x0400,
			DEFERERASE = 0x2000,
			ASYNCWINDOWPOS = 0x4000,
		}
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, SetWindowPosFlags uFlags);

		public enum PROCESS_DPI_AWARENESS
		{
			Process_DPI_Unaware = 0,
			Process_System_DPI_Aware = 1,
			Process_Per_Monitor_DPI_Aware = 2
		}
		#endregion
	}
}