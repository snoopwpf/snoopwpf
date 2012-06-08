// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Snoop
{
	public delegate void EventTrackerHandler(TrackedEvent newEvent);


	/// <summary>
	/// Random class that tries to determine what element handled a specific event.
	/// Doesn't work too well in the end, because the static ClassHandler doesn't get called
	/// in a consistent order.
	/// </summary>
	public class EventTracker : INotifyPropertyChanged, IComparable
	{
		public EventTracker(Type targetType, RoutedEvent routedEvent)
		{
			this.targetType = targetType;
			this.routedEvent = routedEvent;
		}


		public event EventTrackerHandler EventHandled;


		public bool IsEnabled
		{
			get { return this.isEnabled; }
			set
			{
				if (this.isEnabled != value)
				{
					this.isEnabled = value;
					if (this.isEnabled && !this.everEnabled)
					{
						this.everEnabled = true;
						EventManager.RegisterClassHandler(this.targetType, routedEvent, new RoutedEventHandler(this.HandleEvent), true);
					}
					this.OnPropertyChanged("IsEnabled");
				}
			}
		}
		private bool isEnabled;

		public RoutedEvent RoutedEvent
		{
			get { return this.routedEvent; }
		}
		private RoutedEvent routedEvent;

		public string Category
		{
			get { return this.routedEvent.OwnerType.Name; }
		}

		public string Name
		{
			get { return this.routedEvent.Name; }
		}


		private void HandleEvent(object sender, RoutedEventArgs e) 
		{
			// Try to figure out what element handled the event. Not precise.
			if (this.isEnabled) 
			{
				EventEntry entry = new EventEntry(sender, e.Handled);
				if (this.currentEvent != null && this.currentEvent.EventArgs == e) 
				{
					this.currentEvent.AddEventEntry(entry);
				}
				else 
				{
					this.currentEvent = new TrackedEvent(e, entry);
					this.EventHandled(this.currentEvent);
				}
			}
		}



		private TrackedEvent currentEvent = null;
		private bool everEnabled;
		private Type targetType;


		#region IComparable Members
		public int CompareTo(object obj)
		{
			EventTracker otherTracker = obj as EventTracker;
			if (otherTracker == null)
				return 1;

			if (this.Category == otherTracker.Category)
				return this.RoutedEvent.Name.CompareTo(otherTracker.RoutedEvent.Name);
			return this.Category.CompareTo(otherTracker.Category);
		}
		#endregion

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}


	[DebuggerDisplay("TrackedEvent: {EventArgs}")]
	public class TrackedEvent : INotifyPropertyChanged
	{
		public TrackedEvent(RoutedEventArgs routedEventArgs, EventEntry originator)
		{
			this.routedEventArgs = routedEventArgs;
			this.AddEventEntry(originator);
		}


		public RoutedEventArgs EventArgs
		{
			get { return this.routedEventArgs; }
		}
		private RoutedEventArgs routedEventArgs;

		public EventEntry Originator
		{
			get { return this.Stack[0]; }
		}

		public bool Handled
		{
			get { return this.handled; }
			set
			{
				this.handled = value;
				this.OnPropertyChanged("Handled");
			}
		}
		private bool handled = false;

		public object HandledBy
		{
			get { return this.handledBy; }
			set
			{
				this.handledBy = value;
				this.OnPropertyChanged("HandledBy");
			}
		}
		private object handledBy = null;

		public ObservableCollection<EventEntry> Stack
		{
			get { return this.stack; }
		}
		private ObservableCollection<EventEntry> stack = new ObservableCollection<EventEntry>();


		public void AddEventEntry(EventEntry eventEntry)
		{
			this.Stack.Add(eventEntry);
			if (eventEntry.Handled && !this.Handled)
			{
				this.Handled = true;
				this.HandledBy = eventEntry.Handler;
			}
		}


		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}


	public class EventEntry
	{
		public EventEntry(object handler, bool handled)
		{
			this.handler = handler;
			this.handled = handled;
		}

		public bool Handled
		{
			get { return this.handled; }
		}
		private bool handled;

		public object Handler
		{
			get { return this.handler; }
		}
		private object handler;
	}
}
