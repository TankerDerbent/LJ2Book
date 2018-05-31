using System.Text;
using System.Security.Cryptography;

namespace SimplesNet
{
	static class Protector
	{
		public static string Protect(string _Secret)
		{
			// byte[] bytes = Encoding.ASCII.GetBytes(someString);
			// string someString = Encoding.ASCII.GetString(bytes);
			byte[] bytes = Encoding.ASCII.GetBytes(_Secret);
			string sResult = "";

			try
			{
				byte[] result = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
				sResult = Encoding.ASCII.GetString(result);
			}
			catch(CryptographicException)
			{
				//
			}

			return sResult;
		}
		public static string Unprotect(string _Secret)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(_Secret);
			string sResult = "";

			try
			{
				byte[] result = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
				sResult = Encoding.ASCII.GetString(result);
			}
			catch (CryptographicException)
			{
				//
			}

			return sResult;
		}
	}
}
