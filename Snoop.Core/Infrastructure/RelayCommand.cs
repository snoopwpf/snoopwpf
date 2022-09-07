// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System;
using System.Diagnostics;
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Predicate<object?>? canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    [DebuggerStepThrough]
    public bool CanExecute(object? parameter)
    {
        return this.canExecute?.Invoke(parameter) ?? true;
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            if (this.canExecute is not null)
            {
                CommandManager.RequerySuggested += value;
            }
        }

        remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object? parameter)
    {
        this.execute(parameter);
    }
}