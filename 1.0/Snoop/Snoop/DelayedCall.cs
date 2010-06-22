namespace Snoop {
	using System.Windows;
	using System.Windows.Threading;
	
	public delegate void DelayedHandler();

	public class DelayedCall {
		private DelayedHandler handler;
		private DispatcherPriority priority;
		private bool queued;

		public DelayedCall(DelayedHandler handler, DispatcherPriority priority) {
			this.handler = handler;
			this.priority = priority;
		}

		public void Enqueue() {
			if (!this.queued) {
				this.queued = true;
				Application.Current.Dispatcher.BeginInvoke(this.priority, new DispatcherOperationCallback(this.Process), null);
			}
		}

		private object Process(object arg) {
			this.queued = false;

			this.handler();

			return null;
		}

	}
}
