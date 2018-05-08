using System;
using SimplesNet;

namespace LJ2Book
{
	class MainWindowViewModel : BaseViewModel
	{
		public override void Dispose()
		{
			throw new NotImplementedException();
		}

		protected override void CloseApplication()
		{
			base.CloseApplication();
		}
	}
}
