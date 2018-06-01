using System.Text;
using System.Security.Cryptography;
using System;

namespace SimplesNet
{
	static class Protector
	{
		public static string Protect(string _Secret)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(_Secret);
			string sResult = "";

			try
			{
				byte[] result = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
				string sT = result.ToString();
				string sT1 = result[0].ToString();
				sResult = Convert.ToBase64String(result);
			}
			catch(CryptographicException)
			{
				sResult = "";
			}

			return sResult;
		}
		public static string Unprotect(string _Secret)
		{
			string sResult = "";

			try
			{
				byte[] bytes = Convert.FromBase64String(_Secret);
				byte[] result = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
				sResult = Encoding.ASCII.GetString(result);
			}
			catch (FormatException)
			{
				sResult = "";
			}
			catch (CryptographicException)
			{
				sResult = "";
			}

			return sResult;
		}
	}
}
