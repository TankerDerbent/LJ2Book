using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace LJ2Book.LiveJournalAPI
{
	public class ResponceProcessor
	{
		private StringDictionary sd = new StringDictionary();
		public ResponceProcessor(string[] _source)
		{
			for (int i = 0; i < _source.Length - 1; i += 2)
				sd[_source[i]] = _source[i + 1];
		}
		public bool IsOK { get { return sd["success"].ToLower() == "ok"; } }
		public string ErrorMessage { get { return sd["errmsg"]; } }
		public bool TryParseAsGeteventLastnResult(out List<LiveJournalEvent> _result)
		{
			//string[] separator = { Guid.NewGuid().ToString() };
			//source = source.Replace(", ", separator[0]);
			//string[] ss = source.Split(separator, StringSplitOptions.None);
			_result = new List<LiveJournalEvent>();
			if (!IsOK)
				return false;

			int nEventsCount = -1;
			bool bGotEvents = int.TryParse(sd["events_count"], out nEventsCount);
			if (!bGotEvents)
				return false;
			for (int i = 1; i <= nEventsCount; i++)
				_result.Add(new LiveJournalEvent
				{
					anum = Convert.ToInt32(sd[string.Format("events_{0}_anum", i)]),
					ljevent = sd[string.Format("events_{0}_event", i)],
					eventtime = DateTime.Parse(sd[string.Format("events_{0}_eventtime", i)]),
					itemid = Convert.ToInt32(sd[string.Format("events_{0}_itemid", i)]),
					url = sd[string.Format("events_{0}_url", i)],
					Params = new StringDictionary()
				});

			int nPropsCount = -1;
			bool bGotProps = int.TryParse(sd["prop_count"], out nPropsCount);
			if (!bGotProps)
				return true;
			for (int i = 1; i <= nPropsCount; i++)
			{
				int itemid = Convert.ToInt32(sd[string.Format("prop_{0}_itemid", i)]);
				string name = sd[string.Format("prop_{0}_name", i)];
				string value = sd[string.Format("prop_{0}_value", i)];

				//Debug.WriteLine("For itemId={0} prop[{1}]='{2}'", itemid, name, value);

				if ((from lje in _result where lje.itemid == itemid select lje).Count() == 1)
					(from lje in _result where lje.itemid == itemid select lje).First().Params[name] = value;
			}
			return true;
		}
	}

	//*** Response:
	//errmsg
	//Client error:  Invalid destination journal username.
	//success
	//FAIL
	public class LiveJournalEvent
	{
		public int anum { get; set; }
		public string ljevent { get; set; }
		public DateTime eventtime { get; set; }
		public int itemid { get; set; }
		public string url { get; set; }
		public StringDictionary Params { get; set; }
		public override string ToString()
		{
			return string.Format("LJ Event anum:{0} event:'{1}' date/time:'{2}' itemID:{3} url:'{4}'", anum, ljevent, eventtime, itemid, url);
		}
	}
}
