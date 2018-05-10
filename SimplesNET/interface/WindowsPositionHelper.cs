using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace SimplesNet
{
	public class WindowPositionHelper
	{
		public static string RegPath = @"Software\MyApp\";

		public static void SaveSize(Window win)
		{
			RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath + win.Name);
			key.SetValue("Bounds", win.RestoreBounds.ToString());
		}

		public static void SetSize(Window win)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath + win.Name);
			if (key != null)
			{
				Rect bounds = Rect.Parse(key.GetValue("Bounds").ToString());

				win.Top = bounds.Top;
				win.Left = bounds.Left;
				if (win.SizeToContent == SizeToContent.Manual)
				{
					win.Width = bounds.Width;
					win.Height = bounds.Height;
				}
			}
		}
	}
}