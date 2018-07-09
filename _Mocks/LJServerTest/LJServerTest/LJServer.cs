using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

using System.Security;
using System.Net;
using System.IO;
using System.Web;

namespace LJServerTest
{
	public abstract class LJServer
	{
		public abstract string GetChallenge ();
		public abstract string SessionGenerate (string login, string password);

		public abstract void LoginClear (string login, string password);
		public abstract void LoginClearMD5 (string login, string password);
		public abstract void LoginCookies (string login, string password);
		public abstract void LoginChallenge (string login, string password);

		public abstract void PostEventChallenge (string text, string subj,
			string user, string password);

		ILog _log;

		WebProxy _proxy = null;

		public WebProxy Proxy
		{
			get { return _proxy; }
			set { _proxy = value; }
		}

		public LJServer (ILog log)
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
		private string GetPage (string url, CookieCollection cookies)
		{
			HttpWebRequest request =
				(HttpWebRequest)WebRequest.Create (url);

			// Заполняем параметры запроса
			// Здесь мы разрешаем автоматические редиректы
			request.AllowAutoRedirect = true;

			request.Credentials = CredentialCache.DefaultCredentials;
			request.Method = "GET";
			request.CookieContainer = new CookieContainer ();

			if (cookies != null)
			{
				request.CookieContainer.Add (cookies);
			}

			// Заполняем параметры Proxy (_proxy == null, если прокси не используется)
			request.Proxy = _proxy;

			// Получаем класс ответа
			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();

			// Читаем ответ
			Stream responseStream = response.GetResponseStream ();
			StreamReader readStream = new StreamReader (responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd ();

			readStream.Close ();
			response.Close ();

			return currResponse;
		}

		/// <summary>
		/// Получить строку, находящуюся между двумя другими
		/// </summary>
		/// <param name="text"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private string ExctractBetween (string text, string left, string right)
		{
			int leftpos = text.IndexOf (left);
			if (leftpos == -1)
			{
				return "";
			}

			int rightpos = text.IndexOf (right, leftpos + left.Length);
			if (rightpos == -1)
			{
				return "";
			}

			return text.Substring (leftpos + left.Length,
				rightpos - (leftpos + left.Length));
		}

		/// <summary>
		/// Загрузить страницу, для доступа к которой требуется авторизация
		/// </summary>
		/// <param name="url">Адрес страницы</param>
		/// <param name="login">Имя пользователя (логин)</param>
		/// <param name="password">Пароль</param>
		/// <returns>Возвращает код HTML для запрашиваемой страницы</returns>
		public string GetPrivatePage (string url, string login, string password)
		{
			CookieCollection cookies = GetBaseCookie (login, password);

			string res = GetPage (url, cookies);
			return res;
		}

		/// <summary>
		/// Получить базовые cookies для доступа к подзамочным записям
		/// </summary>
		/// <param name="login">Имя пользователя</param>
		/// <param name="password">Пароль</param>
		/// <returns>Возвращает полученные cookies</returns>
		private CookieCollection GetBaseCookie (string login, string password)
		{
			string lj_login_chal = GetChallenge ();

			// Рассчитаем ответ как в методе авторизации challenge / response
			string auth_response = GetAuthResponse (password, lj_login_chal);

			// Строка запроса для отправки через форму
			string textRequest = string.Format ("chal={0}&response={1}&user={2}",
				HttpUtility.UrlEncode (lj_login_chal),
				HttpUtility.UrlEncode (auth_response),
				HttpUtility.UrlEncode (login));			 
			 
			// Выводим в лог посылаемый запрос
			_log.WriteLine ("\r\n*** Request:");
			_log.WriteLine (textRequest);

			byte[] byteArray = Encoding.UTF8.GetBytes (textRequest);

			// Получаем класс запроса
			HttpWebRequest request =
				(HttpWebRequest)WebRequest.Create ("http://www.livejournal.com/login.bml");

			// Заполняем параметры запроса
			request.Method = "POST";

			// Запрещаем автоматический редирект, чтобы сохранить cookies, полученные на первой странице после запроса
			request.AllowAutoRedirect = false;
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = textRequest.Length;
			//request.Referer = "http://www.livejournal.com/login.bml";
			request.UserAgent = "LJServerTest";

			// Очищаем коллекцию от старых cookie и добавляем туда новые
			request.CookieContainer = new CookieContainer ();

			// Заполняем параметры Proxy (_proxy == null, если прокси не используется)
			request.Proxy = _proxy;

			// Отправляем данные запроса
			Stream requestStream = request.GetRequestStream ();
			requestStream.Write (byteArray, 0, textRequest.Length);

			// Получаем класс ответа
			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();

			// Читаем ответ
			Stream responseStream = response.GetResponseStream ();
			StreamReader readStream = new StreamReader (responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd ();

			// Выводим в лог ответ сервера
			_log.WriteLine ("\r\n*** Response:");
			_log.WriteLine (currResponse);

			readStream.Close ();
			response.Close ();

			// Оставим только самые необходимые cookies
			CookieCollection newCollection = new CookieCollection ();
			for (int i = 0; i < response.Cookies.Count; i++)
			{
				if (response.Cookies[i].Name == "ljloggedin" ||
					response.Cookies[i].Name == "ljmastersession")
				{
					newCollection.Add (response.Cookies[i]);
				}
			}

			// Выведем в лог только полезные cookie
			_log.WriteLine ("\r\n*** Cookies:");
			for (int i = 0; i < newCollection.Count; i++)
			{
				_log.WriteLine (newCollection[i].ToString ());
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
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider ();
			byte[] hashBytes = md5.ComputeHash( Encoding.UTF8.GetBytes( text ) );

			StringBuilder sb = new StringBuilder();

			foreach( byte hashByte in hashBytes )
				sb.Append( Convert.ToString( hashByte, 16 ).PadLeft( 2, '0' ) );

			return sb.ToString();
		}

		/// <summary>
		/// Вычисление ответа на оклик
		/// </summary>
		/// <param name="password"></param>
		/// <param name="challenge"></param>
		/// <returns></returns>
		protected string GetAuthResponse (string password, string challenge)
		{
			// md5 от пароля
			string hpass = ComputeMD5 (password);

			string constr = challenge + hpass;
			string auth_response = ComputeMD5 (constr);

			return auth_response;
		}

		protected string SendRequest (string textRequest)
		{
			// Выводим в лог посылаемый запрос
			_log.WriteLine ("\r\n*** Request:");
			_log.WriteLine (textRequest);
			
			// Преобразуем запрос из строки в byte[]
			byte[] byteArray = Encoding.UTF8.GetBytes (textRequest);

			// Поулчаем класс запроса
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (ServerUri);

			// Заполняем параметры запроса
			request.Credentials = CredentialCache.DefaultCredentials;
			request.Method = "POST";
			request.ContentLength = textRequest.Length;
			request.ContentType = this.ContentType;

			// Очищаем коллекцию от старых cookie и добавляем туда новые
			request.CookieContainer = new CookieContainer ();
			request.CookieContainer.Add (_cookies);

			// Заполняем параметры Proxy (_proxy == null, если прокси не используется)
			request.Proxy = _proxy;

			// Если есть cookie с именем "ljsession", то для авторизации с ее помощью
			// необходимо добавить заголовок с именем "X-LJ-Auth" и значением "cookie"
			if (_cookies["ljsession"] != null)
			{
				request.Headers.Add ("X-LJ-Auth", "cookie");
			}

			// Отправляем данные запроса
			Stream requestStream = request.GetRequestStream ();
			requestStream.Write (byteArray, 0, textRequest.Length);

			// Получаем класс ответа
			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();

			// Читаем ответ
			Stream responseStream = response.GetResponseStream ();
			StreamReader readStream = new StreamReader (responseStream, Encoding.UTF8);

			string currResponse = readStream.ReadToEnd ();

			// Выводим в лог ответ сервера
			_log.WriteLine ("\r\n*** Response:");
			_log.WriteLine (currResponse);

			// Выведем в лог полученные в ответе cookie
			_log.WriteLine ("\r\n*** Cookies:");
			for (int i = 0; i < response.Cookies.Count; i++)
			{
				_log.WriteLine (response.Cookies[i].ToString());
			}

			readStream.Close ();
			response.Close ();

			return currResponse;
		}
	}
}
