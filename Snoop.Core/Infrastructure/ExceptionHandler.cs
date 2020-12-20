namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Threading;
    using Snoop.Windows;

    public static class ExceptionHandler
    {
        private static readonly List<WeakReference> knownDispatchers = new List<WeakReference>();

        public static void AddExceptionHandler(Dispatcher dispatcher)
        {
            if (dispatcher is null)
            {
                return;
            }

            if (knownDispatchers.Any(x => x.IsAlive && ReferenceEquals(x.Target, dispatcher)))
            {
                return;
            }

            knownDispatchers.Add(new WeakReference(dispatcher));
            dispatcher.UnhandledException += UnhandledExceptionHandler;
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
}