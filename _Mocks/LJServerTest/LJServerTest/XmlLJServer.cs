using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using System.Net;

namespace LJServerTest
{
	public class XmlLJServer : LJServer
	{
		public XmlLJServer (ILog log)
			: base (log)
		{
		}

		public override void LoginClear (string user, string password)
		{
			string request = string.Format (@"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.login</methodName>
<params>
<param>

<value><struct>
<member><name>username</name>
<value><string>{0}</string></value>
</member>

<member><name>password</name>
<value><string>{1}</string></value>
</member>

<member><name>ver</name>
<value><int>1</int></value>
</member>

</struct></value>
</param>
</params>
</methodCall>",
				user, password);

			string response = this.SendRequest (request);
		}

		public override string ServerUri
		{
			get { return "http://www.livejournal.com/interface/xmlrpc"; }
		}

		public override string ContentType
		{
			get { return "text/xml"; }
		}

		public override void LoginChallenge (string username, string password)
		{
			string challenge = GetChallenge ();
			string auth_response = GetAuthResponse (password, challenge);

			string request = string.Format (@"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.login</methodName>

<params>
<param>

<value><struct>

<member><name>username</name>
<value><string>{0}</string></value>
</member>

<member><name>auth_method</name>
<value><string>challenge</string></value>
</member>

<member><name>auth_challenge</name>
<value><string>{1}</string></value>
</member>

<member><name>auth_response</name>
<value><string>{2}</string></value>
</member>

<member><name>ver</name>
<value><int>1</int></value>
</member>

</struct></value>

</param>
</params>

</methodCall>", username, challenge, auth_response);

			SendRequest (request);
		}

		public override string GetChallenge ()
		{
			string request = @"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.getchallenge</methodName>
<params>
</params>
</methodCall>";

			string challengeResponse = SendRequest (request);

			// ѕолучим значение challenge. 
			// ћежду этими строками находитс€ интересующее нас значение
			string left = "<name>challenge</name><value><string>";
			string right = "</string></value>";

			return ExctractValue (challengeResponse, left, right);
		}

		private static string ExctractValue (string challengeResponse, string left, string right)
		{
			int leftpos = challengeResponse.IndexOf (left);
			if (leftpos == -1)
			{
				return "";
			}

			int rightpos = challengeResponse.IndexOf (right, leftpos + left.Length);
			if (rightpos == -1)
			{
				return "";
			}

			return challengeResponse.Substring (leftpos + left.Length,
				rightpos - (leftpos + left.Length));
		}

		public override void PostEventChallenge (string text, string subj,
			string user, string password)
		{
			string challenge = GetChallenge ();

			string auth_response = GetAuthResponse (password, challenge);

			DateTime date = DateTime.Now;

			/*
			string request = string.Format ("mode=postevent&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&event={3}&subject={4}&allowmask=0&year={5}&mon={6}&day={7}&hour={8}&min={9}&ver=1",
				challenge, 
				auth_response, 
				user, 
				HttpUtility.UrlEncode (text), 
				HttpUtility.UrlEncode (subj),
				date.Year, date.Month, date.Day,
				date.Hour, date.Minute);
			*/

			string request = string.Format (@"<?xml version='1.0' encoding='utf-8'?>
<methodCall>
<methodName>LJ.XMLRPC.postevent</methodName>

<params>
<param>

<value><struct>

<member><name>auth_method</name>
<value><string>challenge</string></value>
</member>

<member><name>auth_challenge</name>
<value><string>{0}</string></value>
</member>

<member><name>auth_response</name>
<value><string>{1}</string></value>
</member>

<member><name>username</name>
<value><string>{2}</string></value>
</member>

<member><name>event</name>
<value><string>{3}</string></value>
</member>

<member><name>subject</name>
<value><string>{4}</string></value>
</member>

<member><name>allowmask</name>
<value><string>0</string></value>
</member>

<member><name>year</name>
<value><string>{5}</string></value>
</member>

<member><name>mon</name>
<value><string>{6}</string></value>
</member>

<member><name>day</name>
<value><string>{7}</string></value>
</member>

<member><name>hour</name>
<value><string>{8}</string></value>
</member>

<member><name>min</name>
<value><string>{9}</string></value>
</member>

<member><name>ver</name>
<value><string>1</string></value>
</member>

<member><name>lineendings</name>
<value><string>{10}</string></value>
</member>

<member><name>prop_current_music</name>
<value><string>{11}</string></value>
</member>

<member><name>ver</name>
<value><string>1</string></value>
</member>

</struct></value>
</param>
</params>

</methodCall>",
				challenge,
				auth_response,
				user,
				text,
				subj,
				date.Year, date.Month, date.Day,
				date.Hour, date.Minute,
				"\r\n",
				"XmlLJServer");

			SendRequest (request);
		}

		public override void LoginCookies (string username, string password)
		{
			string ljsession = SessionGenerate (username, password);

			Cookie cookie = new Cookie ("ljsession", ljsession, "/", "livejournal.com");

			_cookies = new CookieCollection ();
			_cookies.Add (cookie);

			//string request = string.Format ("mode=login&auth_method=cookie&user={0}", login);

			string request = string.Format (@"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.login</methodName>
<params>
<param>

<value><struct>
<member><name>username</name>
<value><string>{0}</string></value>
</member>

<member><name>auth_method</name>
<value><string>cookie</string></value>
</member>

<member><name>ver</name>
<value><int>1</int></value>
</member>

</struct></value>
</param>
</params>
</methodCall>",
				username);

			SendRequest (request);
		}

		public override string SessionGenerate (string user, string password)
		{
			string challenge = GetChallenge ();

			string auth_response = GetAuthResponse (password, challenge);

			//string request = string.Format ("mode=sessiongenerate&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&expiration=long", challenge, auth_response, login);

			string request = string.Format (@"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.sessiongenerate</methodName>
<params>
<param>

<value><struct>
<member><name>auth_method</name>
<value><string>challenge</string></value>
</member>

<member><name>auth_challenge</name>
<value><string>{0}</string></value>
</member>

<member><name>auth_response</name>
<value><string>{1}</string></value>
</member>

<member><name>expiration</name>
<value><string>long</string></value>
</member>

<member><name>username</name>
<value><string>{2}</string></value>
</member>

<member><name>ver</name>
<value><int>1</int></value>
</member>

</struct></value>
</param>
</params>
</methodCall>",
				challenge, auth_response, user);

			string challengeResponse = SendRequest (request);

			// ѕолучим значение challenge. 
			// ћежду этими строками находитс€ интересующее нас значение
			string left = "<name>ljsession</name><value><string>";
			string right = "</string></value>";

			return ExctractValue (challengeResponse, left, right);
		}

		public override void LoginClearMD5 (string user, string password)
		{
			string request = string.Format (@"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.login</methodName>
<params>
<param>

<value><struct>
<member><name>username</name>
<value><string>{0}</string></value>
</member>

<member><name>hpassword</name>
<value><string>{1}</string></value>
</member>

<member><name>ver</name>
<value><int>1</int></value>
</member>

</struct></value>
</param>
</params>
</methodCall>",
				user, ComputeMD5 (password));

			string response = this.SendRequest (request);

		}
	}
}