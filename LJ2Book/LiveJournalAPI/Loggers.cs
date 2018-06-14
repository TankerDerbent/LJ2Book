using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LJ2Book.LiveJournalAPI
{
	public enum LogWhat { Request, Response, Cookies };
	//public interface ILogger
	//{
	//	void Log(LogWhat what, string text);
	//	void Clear();
	//	string[] ToArray();
	//}
	internal class Dialogue
	{
		public string Request { get; set; }
		public List<string> Response { get; set; }
		public List<string> Cookies { get; set; }
	}
	public class Logger //: ILogger
	{
		private List<Dialogue> interactions = new List<Dialogue>();

		private List<Dialogue> Interactions { get => interactions; set => interactions = value; }
		private Dialogue dialogue;

		public void Log(LogWhat what, string text)
		{
			if (what == LogWhat.Request)
			{
				string s = text.Replace("\r", string.Empty).Replace("\n", string.Empty);
				dialogue = new Dialogue { Request = s, Response = new List<string>(), Cookies = new List<string>() };
			}
			else if (what == LogWhat.Response)
			{
				if (dialogue == null)
					throw new InvalidOperationException("You have to log 'Request' before logging 'Response'");

				foreach (var s in Regex.Split(text, @"(?:\r\n|\n|\r)"))
					dialogue.Response.Add(s);
			}
			else //if (what == LogWhat.Cookies)
			{
				if (dialogue == null)
					throw new InvalidOperationException("You have to log 'Request' and 'Response' before logging 'Cookies'");

				foreach (var s in Regex.Split(text, @"(?:\r\n|\n|\r)"))
					dialogue.Cookies.Add(s);

				Interactions.Add(dialogue);
				dialogue = null;
			}
		}
		public void Clear()
		{
			interactions = new List<Dialogue>();
		}
		public string[] ToArray()
		{
			List<string> result = new List<string>();
			foreach (var d in interactions)
			{
				result.Add("*** Request:");
				result.Add(d.Request);
				result.Add("*** Response:");
				foreach (var s in d.Response)
					result.Add(s);
				result.Add("*** Cookies:");
				foreach (var s in d.Cookies)
					result.Add(s);
				result.Add("*** End");
				result.Add("");
			}
			return result.ToArray();
		}
	}


}
