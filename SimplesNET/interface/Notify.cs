using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace SimplesNet
{
	public class Notify : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public static string GetPropertyName<T>(Expression<Func<T>> action)
		{
			var me = action.Body as MemberExpression;

			if (me == null)
			{
				if (action.Body.Type == typeof(string))
					return action.Body.ToString().Trim('\"');
				throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
			}

			return me.Member.Name;
		}

		protected void OnPropertyChanged<T>(Expression<Func<T>> action)
		{
			var propertyName = GetPropertyName(action);
			OnPropertyChanged(propertyName);
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				var e = new PropertyChangedEventArgs(propertyName);
				PropertyChanged(this, e);
			}
		}

		private void OnPropertyChanged(string propertyName, INotifyPropertyChanged model)
		{
			if (model != null)
			{
				var e = new PropertyChangedEventArgs(propertyName);
				FieldInfo fieldInfo = model.GetType().GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
				var delegates = (PropertyChangedEventHandler)fieldInfo.GetValue(model);
				if (delegates != null)
				{
					delegates(model, e);
				}
			}
		}

		private static Notify _notify = new Notify();
		public static bool GetIsPropertyChanged(INotifyPropertyChanged model)
		{
			if (model != null)
			{
				FieldInfo fieldInfo = model.GetType().GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
				var delegates = (PropertyChangedEventHandler)fieldInfo.GetValue(model);
				return delegates != null;
			}
			return false;
		}

		public static void OnPropertyChanged<T>(Expression<Func<T>> action, INotifyPropertyChanged model)
		{
			var propertyName = GetPropertyName(action);
			_notify.OnPropertyChanged(propertyName, model);
		}

		public bool isPropertyChanged { get { return PropertyChanged != null; } }
	}

	public class Notify<T> where T : INotifyPropertyChanged
	{
		T _iModel { get; set; }
		public Notify(T iModel)
		{
			_iModel = iModel;
		}

		public void OnPropertyChanged<Tn>(Expression<Func<Tn>> action)
		{
			Notify.OnPropertyChanged(action, _iModel);
		}

		public bool isPropertyChanged { get { return Notify.GetIsPropertyChanged(_iModel); } }
	}
}
