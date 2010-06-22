namespace Snoop
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;

	public delegate void EventTrackerHandler(TrackedEvent newEvent);

	/// <summary>
	/// Random class that tries to determine what element handled a specific event.
	/// Doesn't work too well in the end, because the static ClassHandler doesn't get called
	/// in a consistent order.
	/// </summary>
	public class EventTracker: INotifyPropertyChanged, IComparable
	{
		public event EventTrackerHandler EventHandled;

		private RoutedEvent routedEvent;
		private TrackedEvent currentEvent = null;
		private bool isEnabled;
		private bool everEnabled;
		private Type targetType;

		public EventTracker(Type targetType, RoutedEvent routedEvent) {
			this.targetType = targetType;
			this.routedEvent = routedEvent;
		}

		public bool IsEnabled {
			get { return this.isEnabled; }
			set {
				if (this.isEnabled != value) {
					this.isEnabled = value;
					if (this.isEnabled && !this.everEnabled) {
						this.everEnabled = true;
						EventManager.RegisterClassHandler(this.targetType, routedEvent, new RoutedEventHandler(this.HandleEvent), true);
					}
					this.OnPropertyChanged("IsEnabled");
				}
			}
		}

		public RoutedEvent RoutedEvent {
			get { return this.routedEvent; }
		}

		private void HandleEvent(object sender, RoutedEventArgs e) {
			// Try to figure out what element handled the event. Not precise.
			if (this.isEnabled) {
				EventEntry entry = new EventEntry(sender, e.Handled);
				if (this.currentEvent != null && this.currentEvent.EventArgs == e) {
					this.currentEvent.AddEventEntry(entry);
				}
				else {
					this.currentEvent = new TrackedEvent(e, entry);
					this.EventHandled(this.currentEvent);
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public int CompareTo(object obj) {
			EventTracker otherTracker = obj as EventTracker;
			if (otherTracker == null)
				return 1;

			if (this.Category == otherTracker.Category)
				return this.RoutedEvent.Name.CompareTo(otherTracker.RoutedEvent.Name);
			return this.Category.CompareTo(otherTracker.Category);
		}

		public string Category {
			get { return this.routedEvent.OwnerType.Name; }
		}

		public string Name {
			get { return this.routedEvent.Name; }
		}
	}

	public class TrackedEvent: INotifyPropertyChanged {
		private ObservableCollection<EventEntry> stack = new ObservableCollection<EventEntry>();

		private RoutedEventArgs routedEventArgs;
		private bool handled = false;
		private object handledBy = null;

		public TrackedEvent(RoutedEventArgs routedEventArgs, EventEntry originator) {
			this.routedEventArgs = routedEventArgs;
			this.AddEventEntry(originator);
		}

		public bool Handled {
			get { return this.handled; }
			set {
				this.handled = value;
				this.OnPropertyChanged("Handled");
			}
		}

		public object HandledBy {
			get { return this.handledBy; }
			set {
				this.handledBy = value;
				this.OnPropertyChanged("HandledBy");
			}
		}

		public EventEntry Originator {
			get { return this.stack[0]; }
		}

		public RoutedEventArgs EventArgs {
			get { return this.routedEventArgs; }
		}
		
		public ObservableCollection<EventEntry> Stack {
			get { return this.stack; }
		}

		public void AddEventEntry(EventEntry eventEntry) {
			this.stack.Add(eventEntry);
			if (eventEntry.Handled && !this.Handled) {
				this.Handled = true;
				this.HandledBy = eventEntry.Handler;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class EventEntry {
		private bool handled;
		private object handler;

		public EventEntry(object handler, bool handled) {
			this.handler = handler;
			this.handled = handled;
		}

		public bool Handled {
			get { return this.handled; }
		}

		public object Handler {
			get { return this.handler; }
		}
	}

	public class ObjectToStringConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			FrameworkElement fe = value as FrameworkElement;
			if (fe != null)
				return fe.Name + " (" + value.GetType().Name + ") ";
			RoutedCommand command = value as RoutedCommand;
			if (command != null)
				return command.Name + " (" + command.GetType().Name + ") ";
			if (value == null)
				return "{null}";
			return "(" + value.GetType().Name + ")";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
