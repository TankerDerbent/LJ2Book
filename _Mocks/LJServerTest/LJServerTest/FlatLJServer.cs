using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using System.Net;

namespace LJServerTest
{
	public class FlatLJServer: LJServer
	{
		public FlatLJServer (ILog log)
			:base (log)
		{
		}

		public override void LoginClear (string user, string password)
		{
			string request = string.Format ("mode=login&auth_method=clear&user={0}&password={1}",
				user, password);

			this.SendRequest (request);
		}

		/// <summary>
		/// Сделать словарь из полученных в ходе запроса значений
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		private StringDictionary MakeDict (string response)
		{
			string[] split = response.Split ("\n".ToCharArray ());

			StringDictionary dict = new StringDictionary();
			for (int i = 0; i < split.Length - 1; i += 2)
			{
				dict[split[i]] = split[i + 1];
			}

			return dict;
		}

		public override string ServerUri
		{
			get { return "http://www.livejournal.com/interface/flat"; }
		}

		public override string ContentType
		{
			get { return "application/x-www-form-urlencoded"; }
		}

		public override void LoginChallenge (string username, string password)
		{
			string challenge = GetChallenge ();

			string auth_response = GetAuthResponse (password, challenge);

			string request = string.Format ("mode=login&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}", challenge, auth_response, username);

			SendRequest (request);
		}

		public override string GetChallenge ()
		{
			string request = "mode=getchallenge";
			string challengeResponse = SendRequest (request);

			StringDictionary dict = MakeDict (challengeResponse);
			if (dict["success"] == "OK")
			{
				return dict["challenge"];
			}

			return "";
		}

		public override void PostEventChallenge (string text, string subj, 
			string user, string password)
		{
			string challenge = GetChallenge ();

			string auth_response = GetAuthResponse (password, challenge);

			DateTime date = DateTime.Now;

			string request = string.Format ("mode=postevent&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&event={3}&subject={4}&allowmask=0&year={5}&mon={6}&day={7}&hour={8}&min={9}&ver=1&prop_current_music={10}&ver=1",
				challenge, 
				auth_response, 
				user, 
				HttpUtility.UrlEncode (text), 
				HttpUtility.UrlEncode (subj), 
				date.Year, date.Month, date.Day, 
				date.Hour, date.Minute,
				HttpUtility.UrlEncode ("FlatLJserver"));

			SendRequest (request);
		}

		public override void LoginCookies (string login, string password)
		{
			string ljsession = SessionGenerate (login, password);

			Cookie cookie = new Cookie ("ljsession", ljsession, "/", "livejournal.com");

			_cookies = new CookieCollection ();
			_cookies.Add (cookie);

			string request = string.Format ("mode=login&auth_method=cookie&user={0}", login);

			SendRequest (request);
		}

		public override string SessionGenerate (string login, string password)
		{
			string challenge = GetChallenge ();

			string auth_response = GetAuthResponse (password, challenge);

			string request = string.Format ("mode=sessiongenerate&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&expiration=long", challenge, auth_response, login);

			string challengeResponse = SendRequest (request);

			StringDictionary dict = MakeDict (challengeResponse);
			if (dict["success"] != "OK")
			{
				return "";
			}

			return dict["ljsession"];
		}

		public override void LoginClearMD5 (string user, string password)
		{
			string request = string.Format ("mode=login&auth_method=clear&user={0}&hpassword={1}",
				user, ComputeMD5 (password) );

			this.SendRequest (request);
		}
	}
}
