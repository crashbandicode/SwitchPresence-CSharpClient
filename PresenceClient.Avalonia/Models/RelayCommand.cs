using System;
using System.Windows.Input;

namespace PresenceClient.Avalonia.Models
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    
    // A simple command implementation for the tray icon menu items.
    public class ActionCommand : ICommand
    {
        private readonly Action _action;
        public event EventHandler? CanExecuteChanged;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
    }
}