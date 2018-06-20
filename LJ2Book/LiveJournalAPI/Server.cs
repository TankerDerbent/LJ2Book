using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace LJ2Book.LiveJournalAPI
{
	public class Connection
	{
		private string _user;
		private string _protectedPassword;
		public string User { get => _user; set => _user = value; }
		public string Pass
		{
			set
			{
				_protectedPassword = SimplesNet.Protector.Protect(value);
			}
		}
		public enum ConnectionStatus { Offline, Connecting, Connected };
		public ConnectionStatus Status { get; set; }
		public Connection()
		{
			this.Status = ConnectionStatus.Offline;
		}
		public Connection(string _user, string _pass, bool _Encrypted = true)
		{
			this.User = _user;
			if (_Encrypted)
				this._protectedPassword = _pass;
			else
			{
				throw new Exception("Unencrypted password usage!");
				//this.Pass = _pass;
			}
			Connect();
		}
		public void Connect()
		{
			Status = ConnectionStatus.Connecting;
			_server.LoginCookies(_user, SimplesNet.Protector.Unprotect(_protectedPassword));
			ResponceProcessor rp = new ResponceProcessor(_server.LastResponse);
			Status = rp.IsOK ? ConnectionStatus.Connected : ConnectionStatus.Offline;
		}
		public bool CheckConnection()
		{
			if (Status == ConnectionStatus.Connected)
				return true;
			Connect();
			return Status == ConnectionStatus.Connected;
		}
		private FlatLJServer _server = new FlatLJServer();
		public bool CheckIsBlogExists(string _Target)
		{
			if (!CheckConnection())
				throw new FailedToRestoreConnectionException(_user);

			string qryGeteventsLastn = string.Format("mode=getevents&auth_method=cookie&selecttype=lastn&howmany=1&user={0}&usejournal={1}", _user, _Target);
			_server.DoCustomQuery(qryGeteventsLastn);

			ResponceProcessor rp = new ResponceProcessor(_server.LastResponse);
			LastError = rp.IsOK ? string.Empty : rp.ErrorMessage;
			return rp.IsOK;
		}
		private string _LastError;
		public string LastError { get => _LastError; internal set => _LastError = value; }
		public class FailedToRestoreConnectionException : Exception
		{
			public FailedToRestoreConnectionException(string _Message) : base(string.Format("Failed to restore connection for login '{0}'", _Message))
			{
				//
			}
		}
		public int GetLastEventNo(string _Target)
		{
			if (!CheckConnection())
				throw new FailedToRestoreConnectionException(_user);

			string qryGeteventsLastn = string.Format("mode=getevents&auth_method=cookie&selecttype=lastn&howmany=1&user={0}&usejournal={1}", _user, _Target);
			_server.DoCustomQuery(qryGeteventsLastn);

			ResponceProcessor rp = new ResponceProcessor(_server.LastResponse);
			LastError = rp.IsOK ? string.Empty : rp.ErrorMessage;

			List<LiveJournalEvent> events;
			rp.TryParseAsGeteventLastnResult(out events);
			if (events != null)
				if (events.Count > 0)
					return events[0].itemid;

			return -1;
		}

		public LiveJournalEvent GetEventByNo(string _Target, int _EventNo)
		{
			if (!CheckConnection())
				throw new FailedToRestoreConnectionException(_user);

			string qryGeteventsLastn = string.Format("mode=getevents&auth_method=cookie&selecttype=one&itemid={2}&user={0}&usejournal={1}", _user, _Target, _EventNo);
			_server.DoCustomQuery(qryGeteventsLastn);

			ResponceProcessor rp = new ResponceProcessor(_server.LastResponse);
			LastError = rp.IsOK ? string.Empty : rp.ErrorMessage;

			if (!rp.IsOK)
				throw new FailedToGetEventByNoException(_Target, _EventNo, LastError);

			List<LiveJournalEvent> events;
			rp.TryParseAsGeteventLastnResult(out events);
			if (events != null)
				if (events.Count == 1)
					return events[0];

			throw new FailedToGetEventByNoException(_Target, _EventNo, string.Format("events.Count = {0}", events.Count));
		}
		public class FailedToGetEventByNoException : Exception
		{
			public FailedToGetEventByNoException(string _Target, int _EventNo, string _AdditionalMessage)
				: base(string.Format("Failed to get event by id for journal '{0}' event no = {1}, more info '{2}'", _Target, _EventNo, _AdditionalMessage))
			{
				//
			}
		}
		public string LoadPrivatePage(string _Url)
		{
			if (!CheckConnection())
				throw new FailedToRestoreConnectionException(_user);

			//return _server.GetPrivatePage(_Url, _user, SimplesNet.Protector.Unprotect(_protectedPassword));
			//if (!_Url.Contains(User + ".livejournal.com"))
			//	throw new FailedToGetPrivatePageException(User, _Url);
			return _server.LoadPrivatePage(_Url);
		}
		//public class FailedToGetPrivatePageException : Exception
		//{
		//	public FailedToGetPrivatePageException(string _User, string _Url)
		//		: base(string.Format("Failed to get private page. Logged in as '{0}', but requesting url {1}", _User, _Url))
		//	{
		//		//
		//	}
		//}
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

		public abstract void PostEventChallenge(string text, string subj, string user, string password);

		//ILogger _log;
		Logger logger = new Logger();

		WebProxy _proxy = null;

		public WebProxy Proxy
		{
			get { return _proxy; }
			set { _proxy = value; }
		}

		public LJServer(/*ILogger log*/)
		{
			//logger = log;
		}

		protected CookieCollection _cookies = new CookieCollection();
		private string[] _lastResponse;

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
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

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
		/// Получить последний item id
		/// </summary>
		/// <param name="usejournal">указать журнал, для которого надо получить item id</param>
		public int GetLastItemID(string usejournal = "")
		{
			int result = -1;
			return result;
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
			//_log.WriteLine("\r\n*** Request:");
			//_log.WriteLine(textRequest);
			logger.Log(LogWhat.Request, textRequest);

			byte[] byteArray = Encoding.UTF8.GetBytes(textRequest);

			// Получаем класс запроса
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.livejournal.com/login.bml");

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
			//_log.WriteLine("\r\n*** Response:");
			//_log.WriteLine(currResponse);
			logger.Log(LogWhat.Response, currResponse);

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
			//_log.WriteLine("\r\n*** Cookies:");
			//for (int i = 0; i < newCollection.Count; i++)
			//{
			//	_log.WriteLine(newCollection[i].ToString());
			//}
			List<string> cookies = new List<string>();
			for (int i = 0; i < newCollection.Count; i++)
				cookies.Add(newCollection[i].ToString());
			logger.Log(LogWhat.Cookies, string.Join("\r\n", cookies.ToArray()));

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
		public string LoadPrivatePage(string _Url)
		{
			logger.Log(LogWhat.Request, _Url);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_Url);
			request.AllowAutoRedirect = true;
			request.Credentials = CredentialCache.DefaultCredentials;
			request.Method = "GET";
			request.CookieContainer = new CookieContainer();
			request.CookieContainer.Add(_cookies);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd();
			readStream.Close();
			response.Close();

			return currResponse;
		}

		protected string SendRequest(string textRequest)
		{
			// Выводим в лог посылаемый запрос
			//_log.WriteLine("\r\n*** Request:");
			//_log.WriteLine(textRequest);
			logger.Log(LogWhat.Request, textRequest);

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
			//_log.WriteLine("\r\n*** Response:");
			//_log.WriteLine(currResponse);
			logger.Log(LogWhat.Response, currResponse);

			List<string> resp = new List<string>();
			foreach (var s in Regex.Split(currResponse, @"(?:\r\n|\n|\r)"))
				resp.Add(s);
			_lastResponse = resp.ToArray();

			// Выведем в лог полученные в ответе cookie
			//_log.WriteLine("\r\n*** Cookies:");
			//for (int i = 0; i < response.Cookies.Count; i++)
			//{
			//	_log.WriteLine(response.Cookies[i].ToString());
			//}
			List<string> cookies = new List<string>();
			for (int i = 0; i < response.Cookies.Count; i++)
				cookies.Add(response.Cookies[i].ToString());
			logger.Log(LogWhat.Cookies, string.Join("\r\n", cookies.ToArray()));


			readStream.Close();
			response.Close();

			return currResponse;
		}
		public string[] LastResponse => _lastResponse;
	}

	public class FlatLJServer : LJServer
	{
		public FlatLJServer()
		//: base(new Logger())
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

		public override void PostEventChallenge(string text, string subj, string user, string password)
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
		public XmlLJServer(/*ILogger log*/)
		//: base(log)
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
