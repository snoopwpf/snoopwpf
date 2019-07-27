using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace Snoop.Infrastructure
{
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
