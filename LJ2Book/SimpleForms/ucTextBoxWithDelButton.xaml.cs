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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LJ2Book.SimpleForms
{
	/// <summary>
	/// Interaction logic for ucTextBoxWithDelButton.xaml
	/// </summary>
	public partial class ucTextBoxWithDelButton : UserControl
	{
		public ucTextBoxWithDelButton()
		{
			InitializeComponent();
		}
		public ICommand CommandDelete { get => (ICommand)GetValue(CommandDeleteProperty); set => SetValue(CommandDeleteProperty, value); }
		public static readonly DependencyProperty CommandDeleteProperty = DependencyProperty.Register("CommandDelete", typeof(ICommand), typeof(ucTextBoxWithDelButton));
		public ICommand CommandEnter { get => (ICommand)GetValue(CommandEnterProperty); set => SetValue(CommandEnterProperty, value); }
		public static readonly DependencyProperty CommandEnterProperty = DependencyProperty.Register("CommandEnter", typeof(ICommand), typeof(ucTextBoxWithDelButton));
		public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
		public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(ucTextBoxWithDelButton), new PropertyMetadata(null));
		public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ucTextBoxWithDelButton), new PropertyMetadata(string.Empty));
		public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
		public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ucTextBoxWithDelButton), new PropertyMetadata(true));

		private void DelButton_Click(object sender, RoutedEventArgs e)
		{
			this.Text = string.Empty;
			if (CommandDelete != null)
			{
				if (CommandParameter != null)
				{
					if (CommandDelete.CanExecute(CommandParameter))
						CommandDelete.Execute(CommandParameter);
				}
				else
					if (CommandDelete.CanExecute(null))
					CommandDelete.Execute(null);
			}
		}

		private void TxtValue_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				if (CommandEnter != null)
					if (CommandEnter.CanExecute(null))
						CommandEnter.Execute(null);
		}
	}
}
