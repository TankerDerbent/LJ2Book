using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LJ2Book.SimpleForms
{
	/// <summary>
	/// Interaction logic for AddBlog.xaml
	/// </summary>
	public partial class AddBlog : Window
	{
		public AddBlog()
		{
			InitializeComponent();

			var context = App.db;

			List<string> sNonBloggers = new List<string>();
			var qryNonBloggers = from u in context.Users select u.UserName;
			sNonBloggers = qryNonBloggers.ToList();
			this.DataContext = sNonBloggers;

			this.Loaded += AddBlog_Loaded;
		}

		private void AddBlog_Loaded(object sender, RoutedEventArgs e)
		{
			ctrlCombo.Focus();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
