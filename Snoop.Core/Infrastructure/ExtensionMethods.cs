// ReSharper disable once CheckNamespace
namespace Snoop;

using System;
using System.Windows.Threading;

public static class ExtensionMethods
{
    private const DispatcherPriority DefaultDispatcherPriority = DispatcherPriority.Normal;

    public static void RunInDispatcherAsync(this DispatcherObject? dispatcher, Action action, DispatcherPriority priority = DefaultDispatcherPriority)
    {
        if (dispatcher is null)
        {
            action();
            return;
        }

        dispatcher.Dispatcher.RunInDispatcherAsync(action, priority);
    }

    public static void RunInDispatcherAsync(this Dispatcher? dispatcher, Action action, DispatcherPriority priority = DefaultDispatcherPriority)
    {
        if (dispatcher is null)
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(priority, action);
        }
    }

    public static void RunInDispatcher(this DispatcherObject? dispatcher, Action action, DispatcherPriority priority = DefaultDispatcherPriority)
    {
        if (dispatcher is null)
        {
            action();
            return;
        }

        dispatcher.Dispatcher.RunInDispatcher(action, priority);
    }

    public static void RunInDispatcher(this Dispatcher? dispatcher, Action action, DispatcherPriority priority = DefaultDispatcherPriority)
    {
        if (dispatcher is null
            || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(priority, action);
        }
    }

    public static T RunInDispatcher<T>(this DispatcherObject? dispatcher, Func<T> action, DispatcherPriority priority = DefaultDispatcherPriority)
    {
        if (dispatcher is null)
        {
            return action();
        }

        return dispatcher.Dispatcher.RunInDispatcher(action, priority);
    }

    public static T RunInDispatcher<T>(this Dispatcher? dispatcher, Func<T> action, DispatcherPriority priority = DefaultDispatcherPriority)
    {
        if (dispatcher is null
            || dispatcher.CheckAccess())
        {
            return action();
        }

        return (T)dispatcher.Invoke(priority, action);
    }
}