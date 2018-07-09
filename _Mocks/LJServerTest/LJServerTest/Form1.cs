using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace LJServerTest
{
	public partial class mainForm : Form
	{
		FlatLJServer _flatServer;
		XmlLJServer _xmlServer;

		//ILog _requestLog;
		//ILog _responseLog;

		ILog _log;

		public mainForm()
		{
			InitializeComponent();

			protocol.Items.Add("Flat");
			protocol.Items.Add("XML-RPC");
			protocol.SelectedIndex = 0;

			_log = new TextLog(logText);

			_flatServer = new FlatLJServer(_log);
			_xmlServer = new XmlLJServer(_log);
		}

		private void login_Click(object sender, EventArgs e)
		{
			GetServer().LoginClear(loginText.Text, passwordText.Text);
		}

		private WebProxy GetProxy()
		{
			WebProxy proxy = null;

			if (useProxy.Checked)
			{
				proxy = new WebProxy(proxyUrlText.Text, int.Parse(proxyPortText.Text));
			}

			return proxy;
		}

		private LJServer GetServer()
		{
			LJServer currentServer;
			if (protocol.SelectedIndex == 0)
			{
				currentServer = _flatServer;
			}
			else
			{
				currentServer = _xmlServer;
			}

			currentServer.Proxy = GetProxy();
			return currentServer;
		}

		private void loginChallenge_Click(object sender, EventArgs e)
		{
			GetServer().LoginChallenge(loginText.Text, passwordText.Text);
		}

		private void postBtn_Click(object sender, EventArgs e)
		{
			if (protocol.SelectedIndex == 0)
			{
				GetServer().PostEventChallenge("Этот пост отправлен из тестовой программы",
					"Проверка",
					loginText.Text,
					passwordText.Text);
			}
			else
			{
				GetServer().PostEventChallenge("Test",
					"Test",
					loginText.Text,
					passwordText.Text);
			}

			/*GetServer ().PostEventChallenge ("Этот пост отправлен из тестовой программы",
					"Проверка",
					loginText.Text,
					passwordText.Text);*/
		}

		private void loginCookies_Click(object sender, EventArgs e)
		{
			GetServer().LoginCookies(loginText.Text, passwordText.Text);
		}

		private void loginClearMD5_Click(object sender, EventArgs e)
		{
			GetServer().LoginClearMD5(loginText.Text, passwordText.Text);
		}

		private void clear_Click(object sender, EventArgs e)
		{
			logText.Text = "";
		}

		private void privateBtn_Click(object sender, EventArgs e)
		{
			string page = GetServer().GetPrivatePage(urlText.Text,
				loginText.Text, passwordText.Text);

			PageViewer dlg = new PageViewer(page);
			dlg.ShowDialog();
		}
	}
}