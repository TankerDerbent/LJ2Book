using SimplesNet;
using System.Windows;

namespace LJ2Book.FormBrowseBlog
{
	class BrowseBlogViewModel : BaseViewModel
	{
		private LJ2Book.MainWindowViewModel RootVM;

		public BrowseBlogViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			RootVM = _RootVM;
		}

		public override void Dispose()
		{
			//throw new NotImplementedException();
		}
	}
}
