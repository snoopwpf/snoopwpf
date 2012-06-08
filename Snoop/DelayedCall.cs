// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop
{
	public delegate void DelayedHandler();

	public class DelayedCall
	{
		public DelayedCall(DelayedHandler handler, DispatcherPriority priority)
		{
			this.handler = handler;
			this.priority = priority;
		}

		public void Enqueue()
		{
			if (!this.queued)
			{
				this.queued = true;

				Dispatcher dispatcher = null;
				if (Application.Current == null || SnoopModes.MultipleDispatcherMode)
					dispatcher = Dispatcher.CurrentDispatcher;
				else
					dispatcher = Application.Current.Dispatcher;

				dispatcher.BeginInvoke(this.priority, new DispatcherOperationCallback(this.Process), null);
			}
		}


		private object Process(object arg)
		{
			this.queued = false;

			this.handler();

			return null;
		}

		private DelayedHandler handler;
		private DispatcherPriority priority;

		private bool queued;
	}
}
