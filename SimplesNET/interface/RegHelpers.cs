
using System;
using Microsoft.Win32;
using System.Globalization;


namespace SimplesNet {

	public static class RegHelpers
	{
		private static string m_registry_root = string.Format("Software\\TS Support\\{0}\\", ProductPathInfo.ProductPathInfo.ProductName);

		static public bool ReadRegistryString(string regSubPath, string name, ref string value, string valueDefault = "")
		{
			if (string.IsNullOrEmpty(name)) return false;
			string _reg_full_path = m_registry_root + regSubPath;

			bool result = false;
			try
			{
				RegistryKey key = null;
				key = Registry.CurrentUser.OpenSubKey(_reg_full_path, RegistryKeyPermissionCheck.Default, System.Security.AccessControl.RegistryRights.SetValue | System.Security.AccessControl.RegistryRights.QueryValues);
				if (key == null)
				{
					Registry.CurrentUser.CreateSubKey(_reg_full_path);
					return false;
				}
				try
				{
					value = key.GetValue(name) as string;
					if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(valueDefault))
					{
						value = valueDefault;
						SaveRegistryString(regSubPath, name, valueDefault);
					}
					result = !string.IsNullOrWhiteSpace(value);
				}
				catch (Exception)
				{
				}
			}
			catch (Exception)
			{
			}

			return result;
		}

		static public bool SaveRegistryString(string regSubPath, string name, string value)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value)) return false;
			string _reg_full_path = m_registry_root + regSubPath;

			bool result = false;
			try
			{
				RegistryKey key = null;
				key = Registry.CurrentUser.OpenSubKey(_reg_full_path, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.SetValue);
				if (key == null)
				{
					key = Registry.CurrentUser.CreateSubKey(_reg_full_path);
				}
				if (key != null)
				{
					key.SetValue(name, value, RegistryValueKind.String);
					result = true;
				}
			}
			catch (Exception)
			{
			}

			return result;
		}

		static public bool ReadRegistryDouble(string regSubPath, string name, ref double value)
		{
			if (string.IsNullOrEmpty(name)) return false;
			string _reg_full_path = m_registry_root + regSubPath;

			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ",";

			bool result = false;
			try
			{
				RegistryKey key = null;
				key = Registry.CurrentUser.OpenSubKey(_reg_full_path, RegistryKeyPermissionCheck.Default, System.Security.AccessControl.RegistryRights.SetValue | System.Security.AccessControl.RegistryRights.QueryValues);
				if (key == null)
				{
					Registry.CurrentUser.CreateSubKey(_reg_full_path);
					return false;
				}
				try
				{
					var val = key.GetValue(name);
					if (result = val != null)
					{
						double dTmp = 0;
						if (result = double.TryParse(val.ToString(), NumberStyles.Number, nfi, out dTmp))
							value = dTmp;
					}
				}
				catch (Exception)
				{
				}
			}
			catch (Exception)
			{
			}

			return result;
		}

		static public bool ReadRegistryKeysBySubPath(string regSubPath, ref string[] value)
		{
			string _reg_full_path = m_registry_root + regSubPath;

			bool result = false;
			try
			{
				RegistryKey key = null;
				key = Registry.CurrentUser.OpenSubKey(_reg_full_path, RegistryKeyPermissionCheck.Default, System.Security.AccessControl.RegistryRights.EnumerateSubKeys | System.Security.AccessControl.RegistryRights.QueryValues);
				if (key != null)
				{
					value = key.GetSubKeyNames();
				}
			}
			catch (Exception ex)
			{
				var str = ex.Message;
			}
			return result;
		}
		
	}
}

