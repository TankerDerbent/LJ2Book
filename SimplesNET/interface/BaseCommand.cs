using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SimplesNet
{
	public class BaseCommand : ICommand
	{
		private readonly Action _command;
		private readonly Action<object> _commandParam;
		private readonly Func<bool> _canExecute;

		public BaseCommand(Action command, Func<bool> canExecute = null)
		{
			if (command == null)
				throw new ArgumentNullException("command");
			_canExecute = canExecute;
			_command = command;
		}

		public BaseCommand(Action<object> command, Func<bool> canExecute = null)
		{
			if (command == null)
				throw new ArgumentNullException("command");
			_canExecute = canExecute;
			_commandParam = command;
		}

		public void Execute(object parameter)
		{
			if (_command != null)
				_command();
			else
				_commandParam(parameter);
		}

		public bool CanExecute(object parameter)
		{
			if (_canExecute == null)
				return true;
			return _canExecute();
		}

		public event EventHandler CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
			}
			remove
			{
				CommandManager.RequerySuggested -= value;
			}
		}
	}
}
