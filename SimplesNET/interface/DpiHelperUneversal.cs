using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimplesNet
{
	namespace DPIAware
	{
		unsafe internal class DpiHelper
		{
			private PROCESS_DPI_AWARENESS _awarness = PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE;
			private Int32 _dpiX = 0;
			private Int32 _dpiY = 0;
			private Int32 _scaleX = 0;
			private Int32 _scaleY = 0;

			public PROCESS_DPI_AWARENESS Awarness { get { return _awarness; } }
			public Int32 DpiX { get { return _dpiX; } }
			public Int32 DpiY { get { return _dpiY; } }
			public Int32 ScaleX { get { return _scaleX; } }
			public Int32 ScaleY { get { return _scaleY; } }
			public double PixelSize { get { return 96.0 / 175.0; } }

			public DpiHelper()
			{
				try
				{
					Process currentProcess = Process.GetCurrentProcess();

					fixed (PROCESS_DPI_AWARENESS* awarness = &_awarness)
					{
						GetProcessDpiAwareness(currentProcess.Handle.ToPointer(), awarness);
					}

					if (Awarness != PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE)
						throw new SystemException("This class can be used only in System-Aware process");

					var pt = new POINTSTRUCT(0, 0);
					var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

					GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _dpiX, out _dpiY);
					_scaleX = unchecked((int)(Math.BigMul(100, _dpiX) / DEFAULT_DPI));
					_scaleY = unchecked((int)(Math.BigMul(100, _dpiY) / DEFAULT_DPI));
				}
				catch
				{
				}
				finally
				{
				{
					if (_dpiX == 0)
					{
						_dpiX = 96;
						_dpiY = 96;
						_scaleX = 100;
						_scaleY = 100;
					}
				}
				}
			}

			[DllImport("Shcore.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
			private static extern Int32 GetProcessDpiAwareness(void* hprocess, PROCESS_DPI_AWARENESS* value);

			[DllImport("Shcore.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
			private static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]MONITOR_DPI_TYPE type, [Out]out Int32 dpiX, [Out]out Int32 dpiY);

			[DllImport("User32.dll", ExactSpelling = true)]
			private static extern IntPtr MonitorFromPoint(POINTSTRUCT pt, int flags);

			[StructLayout(LayoutKind.Sequential)]
			private struct POINTSTRUCT
			{
				public int x;
				public int y;
				public POINTSTRUCT(int x, int y)
				{
					this.x = x;
					this.y = y;
				}
			}

			private enum MONITOR_DPI_TYPE
			{
				MDT_EFFECTIVE_DPI = 0,
				MDT_ANGULAR_DPI = 1,
				MDT_RAW_DPI = 2,
				MDT_DEFAULT = MDT_EFFECTIVE_DPI
			}

			public enum PROCESS_DPI_AWARENESS
			{
				PROCESS_DPI_UNAWARE = 0,
				PROCESS_SYSTEM_DPI_AWARE = 1,
				PROCESS_PER_MONITOR_DPI_AWARE = 2
			}

			const int S_OK = 0;
			const int MONITOR_DEFAULTTONEAREST = 2;
			const int E_INVALIDARG = -2147024809;
			const int DEFAULT_DPI = 96;
		}
	}
}