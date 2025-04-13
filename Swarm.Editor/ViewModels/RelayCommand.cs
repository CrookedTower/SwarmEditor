using System;
using System.Windows.Input;

namespace Swarm.Editor.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T?> execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter is T t ? t : default);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter is T t ? t : default);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
} 