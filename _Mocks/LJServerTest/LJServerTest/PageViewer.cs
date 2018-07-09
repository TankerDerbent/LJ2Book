using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LJServerTest
{
	public partial class PageViewer : Form
	{
		public PageViewer (string html)
		{
			InitializeComponent ();

			webBrowser.DocumentText = html;
		}
	}
}