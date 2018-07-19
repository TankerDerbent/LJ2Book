using SimplesNet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LJ2Book.SimpleForms
{
	/// <summary>
	/// Interaction logic for ucTagsList.xaml
	/// </summary>
	public partial class ucTagsList : UserControl
	{
		private LocalVM localVM;
		public ucTagsList()
		{
			localVM = new LocalVM(this);
			InitializeComponent();
			ddButton.DataContext = localVM;
		}
		public string[] ItemsSource { get => (string[])GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(string[]), typeof(ucTagsList),
			new PropertyMetadata(null, OnItemsSourceChanged));
		public string[] SelectedTags { get => (string[])GetValue(SelectedTagsProperty); set => SetValue(SelectedTagsProperty, value); }
		public static readonly DependencyProperty SelectedTagsProperty = DependencyProperty.Register("SelectedTags", typeof(string[]), typeof(ucTagsList),
			new PropertyMetadata(new string[] { }));
		public object ButtonObject { get => ddButton; }
		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucTagsList ctrlTagsList = (ucTagsList)d;
			var t = e.NewValue.GetType();
			if (!(e.NewValue is string[]))
				return;

			string[] tagsItemSource = ((string[])e.NewValue).Distinct().OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToArray();
			while (tagsItemSource.Length > 0)
			{
				if (tagsItemSource[0].Length == 0)
					tagsItemSource = tagsItemSource.Skip(1).ToArray();
				else
					break;
			}
			ctrlTagsList.SetTags(new ObservableCollection<TagItem>(from s in tagsItemSource select new TagItem { Name = s }));
		}
		public void SetTags(ObservableCollection<TagItem> _tags)
		{
			localVM.Tags = _tags;
		}
		public string TagsListText
		{
			get
			{
				ObservableCollection<TagItem> list = (listBox.DataContext) as ObservableCollection<TagItem>;
				if (list.Count() == 0)
					return string.Empty;

				return string.Join(",", (from tag in list where tag.IsChecked select tag.Name).ToArray());
			}
		}

		class LocalVM : BaseViewModel
		{
			private ucTagsList Owner;
			public LocalVM(ucTagsList _owner)
			{
				Owner = _owner;
			}
			private LocalVM() { }
			private ObservableCollection<TagItem> _Tags;
			public ObservableCollection<TagItem> Tags
			{
				get => _Tags;
				set
				{
					_Tags = value;
					OnPropertyChanged(() => Tags);
				}
			}
			private string _TagsBeforeOpen = string.Empty;
			private bool _IsOpen = false;
			public bool IsOpen
			{
				get => _IsOpen;
				set
				{
					_IsOpen = value;
					if (_IsOpen)
						_TagsBeforeOpen = string.Join(",", SelectedTags);
					OnPropertyChanged(() => IsOpen);
				}
			}
			private string[] SelectedTags { get => (from t in _Tags where t.IsChecked == true select t.Name).ToArray(); }
			public ICommand SelectAll
			{
				get
				{
					return new BaseCommand(() => { foreach (var tag in _Tags) tag.IsChecked = true; OnPropertyChanged(() => TagsListText); });
				}
			}
			public ICommand SelectNone
			{
				get
				{
					return new BaseCommand(() => { foreach (var tag in _Tags) tag.IsChecked = false; OnPropertyChanged(() => TagsListText); });
				}
			}
			public ICommand ApplyTags
			{
				get
				{
					return new BaseCommand(() => { IsOpen = false; Owner.SelectedTags = this.SelectedTags; });
				}
			}
			public ICommand Cancel
			{
				get
				{
					return new BaseCommand(() =>
					{
						string[] prevTags = _TagsBeforeOpen.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
						foreach (var tag in Tags)
							tag.IsChecked = prevTags.Contains(tag.Name);
						OnPropertyChanged(() => TagsListText);
						IsOpen = false;
					});
				}
			}
			public ICommand CheckItem { get { return new BaseCommand(x => { OnPropertyChanged(() => SelectedTags); OnPropertyChanged(() => TagsListText); }); } }
			public string TagsListText { get => _Tags == null ? "click to select tags..." : string.Join(",", SelectedTags); }
			public override void Dispose() { }
		}
		public class TagItem : Notify
		{
			public string Name { get; set; }
			private bool _IsChecked = false;
			public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; OnPropertyChanged(() => IsChecked); } }
		}
	}
}
