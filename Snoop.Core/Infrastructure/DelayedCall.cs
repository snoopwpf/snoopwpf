// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System.Windows.Threading;

public delegate void DelayedHandler();

public class DelayedCall
{
    private readonly DelayedHandler handler;
    private readonly DispatcherPriority priority;

    private bool queued;

    public DelayedCall(DelayedHandler handler, DispatcherPriority priority)
    {
        this.handler = handler;
        this.priority = priority;
    }

    public void Enqueue(Dispatcher dispatcher)
    {
        if (this.queued)
        {
            return;
        }

        this.queued = true;

        dispatcher.RunInDispatcherAsync(this.Process, this.priority);
    }

    private void Process()
    {
        this.queued = false;

        this.handler();
    }
}