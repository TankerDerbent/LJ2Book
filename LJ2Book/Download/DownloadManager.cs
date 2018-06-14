using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LJ2Book.LiveJournalAPI;

namespace LJ2Book.Download
{
	public delegate void DoSmth(int n);
	public class DownloadManager
	{
		private Connection _cnn;
		public DownloadManager()
		{
			myEvent += Do;
			//
		}
		public void Authorize(LJ2Book.DataBase.User _user)
		{
			_cnn = new Connection(_user.UserName, _user.Password);
		}

		public DoSmth myEvent { get; set; }

		private void Do(int n)
		{

		}

	}

	static class Worker
	{
		public static void FromThread(DownloadManager mgr)
		{
			mgr.myEvent.Invoke(100);
		}
	}
}
