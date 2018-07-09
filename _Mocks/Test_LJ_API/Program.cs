using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Test_LJ_API
{
    class Program
    {
        static void Main(string[] args)
        {
			// 29.04.2018 16:32
			// 29.04.2018 16:26
			//string sUser = "testdev666";
			//string sPass = "jopa3666joPa";
			//string sTarget = "evo_lutio";
			string sUser = "lazybiker";
			string sPass = "Slonik26";
			string sTarget = "testdev666";

			CollectedLog logger = new CollectedLog();
			FlatLJServer server = new FlatLJServer(logger);
			
			//FlatLJServer server = new FlatLJServer(new ConsoleLog());
			//XmlLJServer server = new XmlLJServer(new ConsoleLog());

			//string qry1 = string.Format("mode=syncitems&user={0}&password={1}&usejournal={2}", sUser, sPass, sTarget);
			//string qry1 = string.Format("mode=syncitems&user={0}&auth_method=noauth", sUser);
			//server.DoCustomQuery(qry1);

			//string qry2 = string.Format("mode=getevents&user={0}&password={1}&selecttype=one&itemid=1&lineendings=unix&usejournal={2}&noprops=1", sUser, sPass, sTarget);
			//string qry2 = string.Format("mode=getevents&user={0}&password={1}&selecttype=syncitems&lastsync=2007-01-01 00:00:00&lineendings=unix&usejournal={2}", sUser, sPass, sTarget);
			string qry2 = string.Format("mode=getevents&user={0}&password={1}&selecttype=syncitems&lineendings=unix&usejournal={2}", sUser, sPass, sTarget);
			//server.DoCustomQuery(qry2);

			//string qry3 = string.Format("mode=login&auth_method=clear&user={0}&password={1}", sUser, sPass);
			//server.DoCustomQuery(qry3);

			/*Console.WriteLine("----- Get Chlg");
			string qryGetChall = string.Format("mode=getchallenge");
			server.DoCustomQuery(qryGetChall);

			ResponseProcessor rp1 = new ResponseProcessor(logger.ToArray());

			string sChallenge = rp1.Results[0].Value;
			string sAuthResp = server.GetAuthResponse(sPass, sChallenge);

			Console.WriteLine("----- Login");
			//string qryLogin = string.Format("mode=login&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}", sChallenge, sAuthResp, sUser);
			string qryLogin = string.Format("mode=sessiongenerate&auth_method=challenge&auth_challenge={0}&auth_response={1}&user={2}&expiration=long", sChallenge, sAuthResp, sUser);
			server.DoCustomQuery(qryLogin);

			ResponseProcessor rp2 = new ResponseProcessor(logger.ToArray());
			string sCookie = rp2.Results[0].Value;

			Console.WriteLine("----- Load Hist");
			string qry1 = string.Format("mode=syncitems&auth_method=cookie&selecttype=syncitems&lastsync=2007-01-01 00:00:00&user={0}", sUser);
			//string qry1 = string.Format("mode=syncitems&user={0}&auth_method=noauth", sUser);
			server.DoCustomQuery(qry1);*/

			server.LoginCookies(sUser, sPass);
			//string qry1 = string.Format("mode=getevents&auth_method=cookie&selecttype=syncitems&lastsync=2007-01-01 00:00:00&user={0}&usejournal={1}", sUser, sTarget);

			//string qryGetEvents = string.Format("mode=getevents&auth_method=cookie&selecttype=lastn&howmany=5&user={0}&usejournal={1}", sUser, sTarget);
			string qryGetEvents = string.Format("mode=getevents&auth_method=cookie&selecttype=one&itemid={2}&user={0}&usejournal={1}", sUser, sTarget, 2.ToString());
			server.DoCustomQuery(qryGetEvents);
			return;

			ResponseProcessor rp = new ResponseProcessor(logger.ToArray());

			var events = rp.GetEvents();

			int nMaxEventID = (from e in events select e).Max(e => e.itemid);

			//Console.WriteLine("----- Get Oneitem");
			//string qryGetItem = string.Format("mode=getevents&auth_method=cookie&selecttype=one&itemid={2}&user={0}&usejournal={1}", sUser, sTarget, events[1].itemid.ToString());
			//server.DoCustomQuery(qryGetItem);
			for (int i = 0; i < 10; i++)
			{
				if (nMaxEventID - i < 1)
					break;
				Console.Write("------------- Get item #{0} from back", i + 1);
				string qryGetItem = string.Format("mode=getevents&auth_method=cookie&selecttype=one&itemid={2}&user={0}&usejournal={1}", sUser, sTarget, (nMaxEventID - i).ToString());
				server.DoCustomQuery(qryGetItem);
				Console.Write("-----//------ Got item #{0} from back\r\n", i + 1);
			}

			//Console.ReadKey();
		}

	}
}
