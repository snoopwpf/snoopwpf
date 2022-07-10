namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Snoop.Windows;

public static class ExceptionHandler
{
    private static readonly List<WeakReference> knownDispatchers = new();

    public static void AddExceptionHandler(Dispatcher? dispatcher)
    {
        if (dispatcher is null)
        {
            return;
        }

        if (knownDispatchers.Any(x => x.IsAlive && ReferenceEquals(x.Target, dispatcher)))
        {
            return;
        }

        knownDispatchers.Add(new(dispatcher));
        dispatcher.UnhandledException -= UnhandledExceptionHandler;
        dispatcher.UnhandledException += UnhandledExceptionHandler;
    }

    public static void RemoveExceptionHandler(Dispatcher? dispatcher)
    {
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.UnhandledException -= UnhandledExceptionHandler;

        var knownDispatcher = knownDispatchers.FirstOrDefault(x => x.IsAlive && ReferenceEquals(x.Target, dispatcher));

        if (knownDispatcher is not null)
        {
            knownDispatchers.Remove(knownDispatcher);
        }

        var deadKnownDispatchers = knownDispatchers.Where(x => x.IsAlive == false).ToList();

        foreach (var deadKnownDispatcher in deadKnownDispatchers)
        {
            knownDispatchers.Remove(deadKnownDispatcher);
        }
    }

    private static void UnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (SnoopModes.IgnoreExceptions)
        {
            return;
        }

        if (SnoopModes.SwallowExceptions)
        {
            e.Handled = true;
            return;
        }

        e.Handled = ErrorDialog.ShowDialog(e.Exception);
    }
}