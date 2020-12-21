// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using JetBrains.Annotations;

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
            this.RoutedEvent = routedEvent;
        }

        public event EventTrackerHandler? EventHandled;

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
                        EventManager.RegisterClassHandler(this.targetType, this.RoutedEvent, new RoutedEventHandler(this.HandleEvent), true);
                    }

                    this.OnPropertyChanged(nameof(this.IsEnabled));
                }
            }
        }

        private bool isEnabled;

#pragma warning disable WPF0107 // Backing member for a RoutedEvent should be static and readonly.
        public RoutedEvent RoutedEvent { get; }
#pragma warning restore WPF0107 // Backing member for a RoutedEvent should be static and readonly.

        public string Category
        {
            get { return this.RoutedEvent.OwnerType.Name; }
        }

        public string Name
        {
            get { return this.RoutedEvent.Name; }
        }

        private void HandleEvent(object sender, RoutedEventArgs e)
        {
            // Try to figure out what element handled the event. Not precise.
            if (this.isEnabled)
            {
                var entry = new EventEntry(sender, e.Handled);
                if (this.currentEvent is not null && this.currentEvent.EventArgs == e)
                {
                    this.currentEvent.AddEventEntry(entry);
                }
                else
                {
                    this.currentEvent = new TrackedEvent(e, entry);
                    this.EventHandled?.Invoke(this.currentEvent);
                }
            }
        }

        private TrackedEvent? currentEvent;
        private bool everEnabled;
        private readonly Type targetType;

        #region IComparable Members
        public int CompareTo(object? obj)
        {
            var otherTracker = obj as EventTracker;
            if (otherTracker is null)
            {
                return 1;
            }

            if (this.Category == otherTracker.Category)
            {
                return this.RoutedEvent.Name.CompareTo(otherTracker.RoutedEvent.Name);
            }

            return this.Category.CompareTo(otherTracker.Category);
        }
        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    [DebuggerDisplay("TrackedEvent: {" + nameof(EventArgs) + "}")]
    public class TrackedEvent : INotifyPropertyChanged
    {
        public TrackedEvent(RoutedEventArgs routedEventArgs, EventEntry originator)
        {
            this.EventArgs = routedEventArgs;
            this.AddEventEntry(originator);
        }

        public RoutedEventArgs EventArgs { get; }

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
                this.OnPropertyChanged(nameof(this.Handled));
            }
        }

        private bool handled;

        public object? HandledBy
        {
            get { return this.handledBy; }

            set
            {
                this.handledBy = value;
                this.OnPropertyChanged(nameof(this.HandledBy));
            }
        }

        private object? handledBy;

        public ObservableCollection<EventEntry> Stack { get; } = new();

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
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private readonly bool handled;

        public object Handler
        {
            get { return this.handler; }
        }

        private readonly object handler;
    }
}
