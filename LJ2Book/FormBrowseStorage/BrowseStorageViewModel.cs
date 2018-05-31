using System.Windows;
using System.Windows.Input;
using SimplesNet;

namespace LJ2Book.FormBrowseStorage
{
	class BrowseStorageViewModel : BaseViewModel
	{
		private LJ2Book.MainWindowViewModel RootVM;

		public BrowseStorageViewModel(LJ2Book.MainWindowViewModel _RootVM, Window window = null) : base(window)
		{
			RootVM = _RootVM;
		}

		public override void Dispose()
		{
			throw new System.NotImplementedException();
		}
	}
}
