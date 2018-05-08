using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace SimplesNET
{
	public static class AssemblyHelper
	{
		public static Assembly Load(String filePath)
		{
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				if (args.Name.Contains(Path.GetFileNameWithoutExtension(filePath)))
				{
					if (args.Name.ToLowerInvariant().Contains("culture="))
					{
						var iSearchCulture = args.Name.ToLowerInvariant().IndexOf("culture=") + 8;
						StringBuilder sb = new StringBuilder();
						while (iSearchCulture + 1 < args.Name.Length)
						{
							sb.Append(args.Name[iSearchCulture++]);
							if (args.Name[iSearchCulture] == ',')
								break;
						}
						var culture = sb.ToString().Trim();
						if (!string.IsNullOrWhiteSpace(culture))
						{
							var targetAssemblyFileName = Path.GetFileName(filePath).Replace(".dll", ".resources.dll");
							string resourceAssemblyPath = Path.Combine(Path.GetDirectoryName(filePath), culture, targetAssemblyFileName);
							if (File.Exists(resourceAssemblyPath))
							{
								return Assembly.LoadFile(resourceAssemblyPath);
							}
						}
					}
				}
				return null;
			};
			if (File.Exists(filePath))
				return Assembly.LoadFile(filePath);
			return null;
		}
	}
}
