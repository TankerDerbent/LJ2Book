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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LJ2Book.FormBrowseBlog
{
    /// <summary>
    /// Interaction logic for BrowseBlogControl.xaml
    /// </summary>
    public partial class BrowseBlogControl : UserControl
    {
        public BrowseBlogControl()
        {
            InitializeComponent();
        }
		//private bool PanelOpened = false;
		private void ButtonSelectTags_Click(object sender, RoutedEventArgs e)
		{
			btnSelectTags.Visibility = Visibility.Collapsed;
			btnApplyTags.Visibility = Visibility.Visible;
			//string storyBoardName = PanelOpened ? "TogglePanelOff" : "TogglePanelOn";
			Storyboard sb = Resources["TogglePanelOn"] as Storyboard;
			if (sb != null)
				sb.Begin(listTags);

			//PanelOpened = !PanelOpened;
		}

		private void ButtonApplyTags_Click(object sender, RoutedEventArgs e)
		{
			btnSelectTags.Visibility = Visibility.Visible;
			btnApplyTags.Visibility = Visibility.Collapsed;
			Storyboard sb = Resources["TogglePanelOff"] as Storyboard;
			if (sb != null)
				sb.Begin(listTags);

			(this.DataContext as BrowseBlogViewModel).ApplyTags();
		}
	}
}
