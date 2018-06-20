using LJ2Book.DataBase;
using System.Data.Entity;
using System.Windows.Controls;

namespace LJ2Book.FormBrowseStorage
{
	/// <summary>
	/// Interaction logic for BrowseStorageControl.xaml
	/// </summary>
	public partial class BrowseStorageControl : UserControl
	{
		public BrowseStorageControl()
		{
			InitializeComponent();
			//this.DataContextChanged += BrowseStorageControl_DataContextChanged;
		}

		private void BrowseStorageControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			var context = ((sender as BrowseStorageControl).DataContext as BrowseStorageViewModel);
			if(context != null)
			{
				context.PropertyChanged += Context_PropertyChanged;
			}
		}

		private void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var vm = sender as BrowseStorageViewModel;
			if(vm != null)
			{
				if (e.PropertyName == SimplesNet.Notify.GetPropertyName(() => vm.Blogs))
				{
					ListBlogs.ItemsSource = null;
					ListBlogs.SetBinding(ListView.ItemsSourceProperty, "Blogs");
					//BindingOperations.GetBindingExpressionBase((ComboBox)sender, ComboBox.ItemsSourceProperty).UpdateTarget();
					//((TextBox)sender).GetBindingExpression(ComboBox.TextProperty).UpdateSource();
				}
			}
		}
	}
}
