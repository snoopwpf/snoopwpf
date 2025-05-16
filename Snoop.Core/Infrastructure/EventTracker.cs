// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;

[DebuggerDisplay("{" + nameof(Id) + "}")]
[Serializable]
public class EventTrackerSettingsItem
{
    // just for serialization
    private EventTrackerSettingsItem()
    {
        this.Id = string.Empty;
    }

    public EventTrackerSettingsItem(string id)
    {
        this.Id = id;
    }

    public string Id { get; set; }

    public bool IsEnabled { get; set; }
}

/// <summary>
/// Random class that tries to determine what element handled a specific event.
/// Doesn't work too well in the end, because the static ClassHandler doesn't get called
/// in a consistent order.
/// </summary>
[DebuggerDisplay("{" + nameof(Id) + "}")]
public class EventTracker : INotifyPropertyChanged, IComparable, IDisposable
{
    public EventTracker(Type targetType, RoutedEvent routedEvent)
    {
        this.targetType = targetType;
        this.RoutedEvent = routedEvent;
        this.Id = this.RoutedEvent.OwnerType.FullName + ";" + this.RoutedEvent.Name;
    }

    public event EventHandler<TrackedEvent>? EventHandled;

    public string Id { get; }

    public bool IsEnabled
    {
        get { return this.isEnabled; }

        set
        {
            if (this.isEnabled != value)
            {
                this.isEnabled = value;

                if (this.isEnabled
                    && this.everEnabled == false)
                {
                    this.everEnabled = true;
                    this.eventRegistration = EventManagerWrapper.Instance.RegisterClassHandler(Dispatcher.CurrentDispatcher, this.targetType, this.RoutedEvent, this.HandleEvent, true);
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
        if (this.isEnabled == false)
        {
            return;
        }

        // Try to figure out what element handled the event. Not precise.
        var entry = new EventEntry(sender, e.Handled);
        if (this.currentEvent is not null && this.currentEvent.EventArgs == e)
        {
            this.currentEvent.AddEventEntry(entry);
        }
        else
        {
            this.currentEvent = new TrackedEvent(e, entry);
            this.EventHandled?.Invoke(this, this.currentEvent);
        }
    }

    private TrackedEvent? currentEvent;
    private bool everEnabled;
    private readonly Type targetType;
    private EventManagerWrapper.EventRegistration? eventRegistration;

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

    public void Dispose()
    {
        if (this.eventRegistration is not null)
        {
            EventManagerWrapper.Instance.RemoveClassHandler(this.eventRegistration);
        }

        this.eventRegistration = null;
    }
}

[DebuggerDisplay("TrackedEvent: {" + nameof(EventArgs) + "}")]
public class TrackedEvent : EventArgs, INotifyPropertyChanged
{
    public TrackedEvent(RoutedEventArgs routedEventArgs, EventEntry originator)
    {
        this.EventArgs = routedEventArgs;
        this.EventArgsText = $"{this.EventArgs.RoutedEvent.Name} ({GetTimestamp(routedEventArgs)})";

        this.AddEventEntry(originator);
        return;

        static string GetTimestamp(RoutedEventArgs eventArgs)
        {
            var timestamp = (eventArgs as InputEventArgs)?.Timestamp;

            return timestamp.HasValue is false
                ? "unknown timestamp"
                : timestamp.Value.ToString();
        }
    }

    public RoutedEventArgs EventArgs { get; }

    public string EventArgsText { get; }

    public EventEntry Originator => this.Stack[0];

    private bool handled;

    public bool Handled
    {
        get { return this.handled; }

        set
        {
            this.handled = value;
            this.OnPropertyChanged(nameof(this.Handled));
        }
    }

    private object? handledBy;

    public object? HandledBy
    {
        get { return this.handledBy; }

        set
        {
            this.handledBy = value;
            this.OnPropertyChanged(nameof(this.HandledBy));
        }
    }

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

public class EventEntry : INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public EventEntry(object handler, bool handled)
    {
        this.Handler = handler;
        this.Handled = handled;
    }

    public bool Handled { get; }

    public object Handler { get; }
}