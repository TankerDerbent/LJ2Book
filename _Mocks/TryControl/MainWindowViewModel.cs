using SimplesNet;
using System.Diagnostics;

namespace TryControl
{
	class MainWindowViewModel
	{
		public MainWindowViewModel()
		{
			Tags = new string[] { "public", "static", "readonly", "DependencyProperty", "TextProperty", "Register", "typeof(string)" };
		}
		public string[] Tags { get; internal set; }
		private string[] _SelectedTags;
		public string[] SelectedTags
		{
			get
			{
				return _SelectedTags;
			}
			set
			{
				_SelectedTags = value;
				Debug.WriteLine(_SelectedTags == null ? "set SelectedTags = null" : "set SelectedTags = " + string.Join(",", _SelectedTags));
			}
		}
	}
}
