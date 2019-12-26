namespace Snoop.Infrastructure
{
    using System;
    using System.Windows.Threading;

    public static class ExtensionMethods
    {
        public static void InvokeActionSafe(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        public static void BeginInvoke(this Dispatcher dispatcher, DispatcherPriority priority, Action action)
        {
            dispatcher.BeginInvoke(priority, action);
        }
    }
}
