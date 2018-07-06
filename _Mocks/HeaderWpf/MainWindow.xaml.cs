using System.Windows;
using System.Windows.Input;

namespace HeaderWpf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			ucHeader.DataContext = new ucHeaderWin10StyleVM(this);

			ucHeaderWin10StyleVM ctx = (ucHeader.DataContext as ucHeaderWin10StyleVM);
			ctx.BackButtonPressed += Ctx_BackButtonPressed;
		}

		private void Ctx_BackButtonPressed()
		{
			//throw new System.NotImplementedException();
			MessageBox.Show("123");
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			(ucHeader.DataContext as ucHeaderWin10StyleVM).BackButtonEnable = true;
		}
	}
}
