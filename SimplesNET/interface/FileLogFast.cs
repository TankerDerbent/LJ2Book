
using System;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;


namespace SimplesNet {

	public static class FileLogFast
	{
		static public void Write(string filename, string message, bool isNew = false)
		{
			if (!Directory.Exists(Path.GetDirectoryName(filename)))
				Directory.CreateDirectory(Path.GetDirectoryName(filename));
			StringBuilder sb = new StringBuilder();
			try
			{
				if (!isNew && File.Exists(filename))
					sb.Append(File.ReadAllText(filename));
			}
			catch (System.Exception)
			{
				
			}
			sb.AppendLine(string.Format("{0} {1}: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), message));
			File.WriteAllText(filename, sb.ToString());
		}

	}
}

