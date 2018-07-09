using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Web;
using System.Linq;

namespace Test_LJ_API
{
	public interface ILog
	{
		void Write(string text);
		void WriteLine(string text);
		void Clear();
	}
	public abstract class LJServer
	{
		public abstract string GetChallenge();
		public abstract string SessionGenerate(string login, string password);

		public abstract void LoginClear(string login, string password);
		public abstract void LoginClearMD5(string login, string password);
		public abstract void LoginCookies(string login, string password);
		public abstract void LoginChallenge(string login, string password);
		public void DoCustomQuery(string qry)
		{
			this.SendRequest(qry);
		}
		public void DoCustomQueryWithCookie(string cookie, string qry)
		{
			this.SendRequest(qry);
		}


		public abstract void PostEventChallenge(string text, string subj,
			string user, string password);

		ILog _log;

		WebProxy _proxy = null;

		public WebProxy Proxy
		{
			get { return _proxy; }
			set { _proxy = value; }
		}

		public LJServer(ILog log)
		{
			_log = log;
		}

		protected CookieCollection _cookies = new CookieCollection();

		/// <summary>
		/// Адрес сервера 
		/// </summary>
		public abstract string ServerUri
		{
			get;
		}

		public abstract string ContentType
		{
			get;
		}

		/// <summary>
		/// Просто загрузить страницу
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private string GetPage(string url, CookieCollection cookies)
		{
			HttpWebRequest request =
				(HttpWebRequest)WebRequest.Create(url);

			// Заполняем параметры запроса
			// Здесь мы разрешаем автоматические редиректы
			request.AllowAutoRedirect = true;

			request.Credentials = CredentialCache.DefaultCredentials;
			request.Method = "GET";
			request.CookieContainer = new CookieContainer();

			if (cookies != null)
			{
				request.CookieContainer.Add(cookies);
			}

			// Заполняем параметры Proxy (_proxy == null, если прокси не используется)
			request.Proxy = _proxy;

			// Получаем класс ответа
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			// Читаем ответ
			Stream responseStream = response.GetResponseStream();
			StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd();

			readStream.Close();
			response.Close();

			return currResponse;
		}

		/// <summary>
		/// Получить строку, находящуюся между двумя другими
		/// </summary>
		/// <param name="text"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private string ExctractBetween(string text, string left, string right)
		{
			int leftpos = text.IndexOf(left);
			if (leftpos == -1)
			{
				return "";
			}

			int rightpos = text.IndexOf(right, leftpos + left.Length);
			if (rightpos == -1)
			{
				return "";
			}

			return text.Substring(leftpos + left.Length,
				rightpos - (leftpos + left.Length));
		}

		/// <summary>
		/// Загрузить страницу, для доступа к которой требуется авторизация
		/// </summary>
		/// <param name="url">Адрес страницы</param>
		/// <param name="login">Имя пользователя (логин)</param>
		/// <param name="password">Пароль</param>
		/// <returns>Возвращает код HTML для запрашиваемой страницы</returns>
		public string GetPrivatePage(string url, string login, string password)
		{
			CookieCollection cookies = GetBaseCookie(login, password);

			string res = GetPage(url, cookies);
			return res;
		}

		/// <summary>
		/// Получить базовые cookies для доступа к подзамочным записям
		/// </summary>
		/// <param name="login">Имя пользователя</param>
		/// <param name="password">Пароль</param>
		/// <returns>Возвращает полученные cookies</returns>
		private CookieCollection GetBaseCookie(string login, string password)
		{
			string lj_login_chal = GetChallenge();

			// Рассчитаем ответ как в методе авторизации challenge / response
			string auth_response = GetAuthResponse(password, lj_login_chal);

			// Строка запроса для отправки через форму
			string textRequest = string.Format("chal={0}&response={1}&user={2}",
				HttpUtility.UrlEncode(lj_login_chal),
				HttpUtility.UrlEncode(auth_response),
				HttpUtility.UrlEncode(login));

			// Выводим в лог посылаемый запрос
			_log.WriteLine("\r\n*** Request:");
			_log.WriteLine(textRequest);

			byte[] byteArray = Encoding.UTF8.GetBytes(textRequest);

			// Получаем класс запроса
			HttpWebRequest request =
				(HttpWebRequest)WebRequest.Create("http://www.livejournal.com/login.bml");

			// Заполняем параметры запроса
			request.Method = "POST";

			// Запрещаем автоматический редирект, чтобы сохранить cookies, полученные на первой странице после запроса
			request.AllowAutoRedirect = false;
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = textRequest.Length;
			//request.Referer = "http://www.livejournal.com/login.bml";
			request.UserAgent = "LJServerTest";

			// Очищаем коллекцию от старых cookie и добавляем туда новые
			request.CookieContainer = new CookieContainer();

			// Заполняем параметры Proxy (_proxy == null, если прокси не используется)
			request.Proxy = _proxy;

			// Отправляем данные запроса
			Stream requestStream = request.GetRequestStream();
			requestStream.Write(byteArray, 0, textRequest.Length);

			// Получаем класс ответа
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			// Читаем ответ
			Stream responseStream = response.GetResponseStream();
			StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd();

			// Выводим в лог ответ сервера
			_log.WriteLine("\r\n*** Response:");
			_log.WriteLine(currResponse);

			readStream.Close();
			response.Close();

			// Оставим только самые необходимые cookies
			CookieCollection newCollection = new CookieCollection();
			for (int i = 0; i < response.Cookies.Count; i++)
			{
				if (response.Cookies[i].Name == "ljloggedin" ||
					response.Cookies[i].Name == "ljmastersession")
				{
					newCollection.Add(response.Cookies[i]);
				}
			}

			// Выведем в лог только полезные cookie
			_log.WriteLine("\r\n*** Cookies:");
			for (int i = 0; i < newCollection.Count; i++)
			{
				_log.WriteLine(newCollection[i].ToString());
			}

			return newCollection;
		}

		/// <summary>
		/// Посчитать md5. Взято у lj.net. http://lj-net.cvs.sourceforge.net/lj-net/lj-net/Utils.cs?view=markup
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		protected string ComputeMD5(string text)
		{
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));

			StringBuilder sb = new StringBuilder();

			foreach (byte hashByte in hashBytes)
				sb.Append(Convert.ToString(hashByte, 16).PadLeft(2, '0'));

			return sb.ToString();
		}

		/// <summary>
		/// Вычисление ответа на оклик
		/// </summary>
		/// <param name="password"></param>
		/// <param name="challenge"></param>
		/// <returns></returns>
		public string GetAuthResponse(string password, string challenge)
		{
			// md5 от пароля
			string hpass = ComputeMD5(password);

			string constr = challenge + hpass;
			string auth_response = ComputeMD5(constr);

			return auth_response;
		}

		protected string SendRequest(string textRequest)
		{
			// Выводим в лог посылаемый запрос
			_log.WriteLine("\r\n*** Request:");
			_log.WriteLine(textRequest);

			// Преобразуем запрос из строки в byte[]
			byte[] byteArray = Encoding.UTF8.GetBytes(textRequest);

			// Поулчаем класс запроса
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ServerUri);

			// Заполняем параметры запроса
			request.Credentials = CredentialCache.DefaultCredentials;
			request.Method = "POST";
			request.ContentLength = textRequest.Length;
			request.ContentType = this.ContentType;

			// Очищаем коллекцию от старых cookie и добавляем туда новые
			request.CookieContainer = new CookieContainer();
			request.CookieContainer.Add(_cookies);

			// Заполняем параметры Proxy (_proxy == null, если прокси не используется)
			request.Proxy = _proxy;

			// Если есть cookie с именем "ljsession", то для авторизации с ее помощью
			// необходимо добавить заголовок с именем "X-LJ-Auth" и значением "cookie"
			if (_cookies["ljsession"] != null)
			{
				request.Headers.Add("X-LJ-Auth", "cookie");
			}

			// Отправляем данные запроса
			Stream requestStream = request.GetRequestStream();
			requestStream.Write(byteArray, 0, textRequest.Length);

			// Получаем класс ответа
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			// Читаем ответ
			Stream responseStream = response.GetResponseStream();
			StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd();

			// Выводим в лог ответ сервера
			_log.WriteLine("\r\n*** Response:");
			_log.WriteLine(currResponse);

			// Выведем в лог полученные в ответе cookie
			_log.WriteLine("\r\n*** Cookies:");
			for (int i = 0; i < response.Cookies.Count; i++)
			{
				_log.WriteLine(response.Cookies[i].ToString());
			}

			readStream.Close();
			response.Close();

			return currResponse;
		}
	}

	public class QuietLog : ILog
	{
		List<string> mStorage;

		public QuietLog(/*TextBox field*/)
		{
			//_field = field;
			mStorage = new List<string>();
		}

		#region ILog Members

		public void Write(string text)
		{
			//_field.Text = _field.Text + text.Replace("\n", "\r\n");
			string[] sSeparators = { "\r\n" };
			string[] lines = text.Replace("\n", "\r\n").Split(sSeparators, StringSplitOptions.RemoveEmptyEntries);
			bool bFirst = true;
			foreach (var s in lines)
			{
				if (bFirst)
				{
					if (mStorage.Count > 0)
					{
						mStorage[mStorage.Count - 1] += s;
					}
					else
					{
						mStorage.Add(s);
					}
					bFirst = false;
					continue;
				}
				
				mStorage.Add(s);
			}
		}

		public void Clear()
		{
			//_field.Text = "";
			mStorage.Clear();
		}

		#endregion

		#region ILog Members


		public void WriteLine(string text)
		{
			//_field.Text = _field.Text + text.Replace("\n", "\r\n") + "\r\n";
			Write(text + "\r\n");
		}

		public void Dump()
		{
			foreach (var s in mStorage)
				Console.WriteLine(s);
		}

		#endregion
	}
	public class CollectedLog : ILog
	{
		private bool _Collect = false;
		public bool Collect
		{
			get
			{
				return _Collect;
			}
			set
			{
				_Collect = value;
				if (_Collect)
					Responce.Clear();
			}
		}
		private ConsoleLog consoleLogger = new ConsoleLog();
		private List<string> Responce = new List<string>();
		public CollectedLog()
		{
			//
		}

		#region ILog Members
		public void Write(string text)
		{
			//string s1 = text;//.Replace("\r", string.Empty).Replace("\n", string.Empty);
			foreach (var s in Regex.Split(text, @"(?:\r\n|\n|\r)"))
			{
				if (s.StartsWith("*** Cookies:"))
					Collect = false;

				if (Collect)
					Responce.Add(s);

				if (s.StartsWith("*** Response:"))
					Collect = true;

				consoleLogger.Write(s + "\r\n");
			}
		}

		public void Clear()
		{
			consoleLogger.Clear();
			Responce.Clear();
		}

		public void WriteLine(string text)
		{
			string s = text;
			if (text.StartsWith("\r\n"))
				s = text.Substring(2);

			if (s.IndexOf("\r\n") == -1)
				s = s.Replace("\n", "\r\n");

			//Write(s + "\r\n");
			Write(s);
		}
		#endregion
		public void Dump()
		{
			foreach (var s in Responce)
				Write(s);
		}
		public string[] ToArray()
		{
			return Responce.ToArray();
		}
	}

	public class ConsoleLog : ILog
	{
		public ConsoleLog()
		{
		}

		#region ILog Members

		public void Write(string text)
		{
			Console.Write(text.Replace("\n", "\r\n"));
		}

		public void Clear()
		{
		}

		#endregion

		#region ILog Members


		public void WriteLine(string text)
		{
			//_field.Text = _field.Text + text.Replace("\n", "\r\n") + "\r\n";
			Write(text + "\r\n");
		}

		#endregion
	}

	class ResponseProcessor
	{
		public class NodeResult
		{
			public string Name { get; set; }
			public string Value { get; set; }
			public override string ToString()
			{
				return string.Format("Result '{0}' = '{1}'", Name, Value);
			}
		}
		public class NodeCounter
		{
			public string CountWhat { get; set; }
			public string CountHow { get; set; }
			public int Value { get; set; }
			public override string ToString()
			{
				return string.Format("{0} of {1} = {2}", CountHow, CountWhat, Value.ToString());
			}
		}
		public class NodeValue
		{
			public string Name { get; set; }
			public string Property { get; set; }
			public string Value { get; set; }
			public override string ToString()
			{
				return string.Format("{0}'s {1} = {2}", Name, Property, Value);
			}
		}
		public class NodeParam
		{
			public string Name { get; set; }
			public int No { get; set; }
			public List<KeyValuePair<string, string>> Items { get; set; }
			public NodeParam()
			{
				Name = "";
				No = -1;
				Items = new List<KeyValuePair<string, string>>();
			}
		}
		public List<NodeResult> Results { get; set; }
		public List<NodeCounter> Counters { get; set; }
		public List<NodeParam> Params { get; set; }
		public List<NodeValue> Values { get; set; }
		protected string[] RawText;
		private enum ObjType { e1, e2, e3 };
		public ResponseProcessor(string[] _source)
		{
			RawText = _source;

			Results = new List<NodeResult>();
			Counters = new List<NodeCounter>();
			Params = new List<NodeParam>();
			Values = new List<NodeValue>();

			Process();
		}

		private void Process()
		{
			List<string> SourceLines = new List<string>();

			int Len = RawText.Length;
			for (int i = RawText[0].StartsWith("***") ? 1 : 0; (i + 1) < Len; i += 2)
				SourceLines.Add(string.Format("{0}\t{1}", RawText[i], RawText[i + 1]));

			Params.Clear();

			Regex regex = new Regex("([a-z]+)(_((\\d+)_)?([a-z]+))?\\t(.*)");

			foreach (var s in SourceLines)
			{
				Match m = regex.Match(s);

				if (m.Groups.Count != 7)
					throw new InvalidDataException(string.Format("Invalid format of string: '{0}'", s));

				if (m.Groups[4].Value.ToString().Length > 0)
				{
					string sName = m.Groups[1].Value.ToString();
					int paramNo = Convert.ToInt32(m.Groups[4].ToString());
					string fieldName = m.Groups[5].ToString();
					string fieldValue = m.Groups[6].ToString();

					if (Params.Count == 0)
						Params.Add(new NodeParam { Name = sName, No = paramNo, Items = new List<KeyValuePair<string, string>>() });

					if (Params[Params.Count - 1].Name != sName || Params[Params.Count - 1].No != paramNo)
						Params.Add(new NodeParam { Name = sName, No = paramNo, Items = new List<KeyValuePair<string, string>>() });

					NodeParam param = Params[Params.Count - 1];
					param.Items.Add(new KeyValuePair<string, string>(fieldName, fieldValue));
				}
				else if (m.Groups[5].Value.ToString().Length > 0)
				{
					int n = 0;
					if (Int32.TryParse(m.Groups[6].Value.ToString(), out n))
						Counters.Add(new NodeCounter
						{
							CountWhat = m.Groups[1].Value.ToString(),
							CountHow = m.Groups[5].Value.ToString(),
							Value = n
						});
					else
						Values.Add(new NodeValue
						{
							Name = m.Groups[1].Value.ToString(),
							Property = m.Groups[5].Value.ToString(),
							Value = m.Groups[6].Value.ToString()
						});
				}
				else if (m.Groups[1].Value.ToString().Length > 0)
				{
					Results.Add(new NodeResult
					{
						Name = m.Groups[1].Value.ToString(),
						Value = m.Groups[6].Value.ToString()
					});
				}
				else
					throw new InvalidDataException(string.Format("Invalid format of string: '{0}'", s));

			}
		}
		public class LJEvent
		{
			public int anum { get; set; }
			public string ljevent { get; set; }
			public DateTime eventtime { get; set; }
			public int itemid { get; set; }
			public string url { get; set; }
			public StringDictionary Params {get; set;}
		}
		public List<LJEvent> GetEvents()
		{
			List<LJEvent> events = new List<LJEvent>();
			foreach (var p in Params)
			{
				if (p.Items.Count == 5)
					if (p.Items[0].Key == "anum")
						if (p.Items[1].Key == "event")
							if (p.Items[2].Key == "eventtime")
								if (p.Items[3].Key == "itemid")
									if (p.Items[4].Key == "url")
										events.Add(new LJEvent
										{
											anum = Convert.ToInt32(p.Items[0].Value),
											ljevent = p.Items[1].Value,
											eventtime = DateTime.Parse(p.Items[2].Value),
											itemid = Convert.ToInt32(p.Items[3].Value),
											url = p.Items[4].Value,
											Params = new StringDictionary()
										});
			}

			foreach (var p in Params)
			{
				if (p.Items.Count == 3)
					if (p.Items[0].Key == "itemid")
					{
						int itemid = -1;
						if (int.TryParse(p.Items[0].Value, out itemid))
						{
							var item = (from e in events where e.itemid == itemid select e).First();
							item.Params[p.Items[1].Value] = p.Items[2].Value;
						}
					}
			}
			return events;
		}
	}


	public class FlatLJServer : LJServer
	{
		public FlatLJServer(ILog log)
			: base(log)
		{
		}

		public override void LoginClear(string user, string password)
		{
			string request = string.Format("mode=login&auth_method=clear&user={0}&password={1}",
				user, password);

			this.SendRequest(request);
		}

		//public override void DoCustomQuery(string _qry)
		//{
		//	this.SendRequest(_qry);
		//}

		/// <summary>
		/// Сделать словарь из полученных в ходе запроса значений
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		private StringDictionary MakeDict(string response)
		{
			string[] split = response.Split("\n".ToCharArray());

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

		public override void LoginChallenge(string username, string password)
		{
			string challenge = GetChallenge();

			string auth_response = GetAuthResponse(password, challenge);

			string request = string.Format("mode=login&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}", challenge, auth_response, username);

			SendRequest(request);
		}

		public override string GetChallenge()
		{
			string request = "mode=getchallenge";
			string challengeResponse = SendRequest(request);

			StringDictionary dict = MakeDict(challengeResponse);
			if (dict["success"] == "OK")
			{
				return dict["challenge"];
			}

			return "";
		}

		public override void PostEventChallenge(string text, string subj,
			string user, string password)
		{
			string challenge = GetChallenge();

			string auth_response = GetAuthResponse(password, challenge);

			DateTime date = DateTime.Now;

			string request = string.Format("mode=postevent&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&event={3}&subject={4}&allowmask=0&year={5}&mon={6}&day={7}&hour={8}&min={9}&ver=1&prop_current_music={10}&ver=1",
				challenge,
				auth_response,
				user,
				HttpUtility.UrlEncode(text),
				HttpUtility.UrlEncode(subj),
				date.Year, date.Month, date.Day,
				date.Hour, date.Minute,
				HttpUtility.UrlEncode("FlatLJserver"));

			SendRequest(request);
		}

		public override void LoginCookies(string login, string password)
		{
			string ljsession = SessionGenerate(login, password);

			Cookie cookie = new Cookie("ljsession", ljsession, "/", "livejournal.com");

			_cookies = new CookieCollection();
			_cookies.Add(cookie);

			string request = string.Format("mode=login&auth_method=cookie&user={0}", login);

			SendRequest(request);
		}

		public override string SessionGenerate(string login, string password)
		{
			string challenge = GetChallenge();

			string auth_response = GetAuthResponse(password, challenge);

			string request = string.Format("mode=sessiongenerate&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&expiration=long", challenge, auth_response, login);

			string challengeResponse = SendRequest(request);

			StringDictionary dict = MakeDict(challengeResponse);
			if (dict["success"] != "OK")
			{
				return "";
			}

			return dict["ljsession"];
		}

		public override void LoginClearMD5(string user, string password)
		{
			string request = string.Format("mode=login&auth_method=clear&user={0}&hpassword={1}",
				user, ComputeMD5(password));

			this.SendRequest(request);
		}
	}

	public class XmlLJServer : LJServer
	{
		public XmlLJServer(ILog log)
			: base(log)
		{
		}

		public override void LoginClear(string user, string password)
		{
			string request = string.Format(@"<?xml version='1.0'?>
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

			string response = this.SendRequest(request);
		}

		public override string ServerUri
		{
			get { return "http://www.livejournal.com/interface/xmlrpc"; }
		}

		public override string ContentType
		{
			get { return "text/xml"; }
		}

		public override void LoginChallenge(string username, string password)
		{
			string challenge = GetChallenge();
			string auth_response = GetAuthResponse(password, challenge);

			string request = string.Format(@"<?xml version='1.0'?>
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

			SendRequest(request);
		}

		public override string GetChallenge()
		{
			string request = @"<?xml version='1.0'?>
<methodCall>
<methodName>LJ.XMLRPC.getchallenge</methodName>
<params>
</params>
</methodCall>";

			string challengeResponse = SendRequest(request);

			// Получим значение challenge. 
			// Между этими строками находится интересующее нас значение
			string left = "<name>challenge</name><value><string>";
			string right = "</string></value>";

			return ExctractValue(challengeResponse, left, right);
		}

		private static string ExctractValue(string challengeResponse, string left, string right)
		{
			int leftpos = challengeResponse.IndexOf(left);
			if (leftpos == -1)
			{
				return "";
			}

			int rightpos = challengeResponse.IndexOf(right, leftpos + left.Length);
			if (rightpos == -1)
			{
				return "";
			}

			return challengeResponse.Substring(leftpos + left.Length,
				rightpos - (leftpos + left.Length));
		}

		public override void PostEventChallenge(string text, string subj,
			string user, string password)
		{
			string challenge = GetChallenge();

			string auth_response = GetAuthResponse(password, challenge);

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

			string request = string.Format(@"<?xml version='1.0' encoding='utf-8'?>
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

			SendRequest(request);
		}

		public override void LoginCookies(string username, string password)
		{
			string ljsession = SessionGenerate(username, password);

			Cookie cookie = new Cookie("ljsession", ljsession, "/", "livejournal.com");

			_cookies = new CookieCollection();
			_cookies.Add(cookie);

			//string request = string.Format ("mode=login&auth_method=cookie&user={0}", login);

			string request = string.Format(@"<?xml version='1.0'?>
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

			SendRequest(request);
		}

		public override string SessionGenerate(string user, string password)
		{
			string challenge = GetChallenge();

			string auth_response = GetAuthResponse(password, challenge);

			//string request = string.Format ("mode=sessiongenerate&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&expiration=long", challenge, auth_response, login);

			string request = string.Format(@"<?xml version='1.0'?>
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

			string challengeResponse = SendRequest(request);

			// Получим значение challenge. 
			// Между этими строками находится интересующее нас значение
			string left = "<name>ljsession</name><value><string>";
			string right = "</string></value>";

			return ExctractValue(challengeResponse, left, right);
		}

		public override void LoginClearMD5(string user, string password)
		{
			string request = string.Format(@"<?xml version='1.0'?>
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
				user, ComputeMD5(password));

			string response = this.SendRequest(request);

		}
	}

}
