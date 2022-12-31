// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using Snoop.Core;
using Snoop.Infrastructure;
using Snoop.Windows;

public partial class EventsView : INotifyPropertyChanged, IDisposable
{
    public static readonly RoutedCommand ClearCommand = new(nameof(ClearCommand), typeof(EventsView));
    public static readonly RoutedCommand ResetEventTrackersToDefaultCommand = new(nameof(ResetEventTrackersToDefaultCommand), typeof(EventsView));

    private int maxEventsDisplayed = 100;

    private ICollectionView? availableEvents;

    public EventsView()
    {
        this.InterestingEvents = new(this.interestingEvents);

        this.InitializeComponent();

        this.UpdateTrackers();

        this.CommandBindings.Add(new CommandBinding(ClearCommand, this.HandleClear));
        this.CommandBindings.Add(new CommandBinding(ResetEventTrackersToDefaultCommand, this.HandleResetEventTrackersToDefault));
    }

    public void UpdateTrackers()
    {
        this.trackers.Clear();

        var sorter = new List<EventTracker>();

        foreach (var routedEvent in EventManager.GetRoutedEvents())
        {
            var tracker = new EventTracker(typeof(UIElement), routedEvent);
            tracker.EventHandled += this.HandleEventHandled;

            sorter.Add(tracker);

            var savedTrackedEvent = Settings.Default.EventTrackers.FirstOrDefault(x => x.Id == tracker.Id);

            if (savedTrackedEvent is not null)
            {
                tracker.IsEnabled = savedTrackedEvent.IsEnabled;
            }
            else if (defaultEvents.Contains(routedEvent))
            {
                tracker.IsEnabled = true;
            }

            tracker.PropertyChanged += this.HandleTrackerOnPropertyChanged;
        }

        sorter.Sort();

        foreach (var tracker in sorter)
        {
            this.trackers.Add(tracker);
        }
    }

    public ReadOnlyObservableCollection<TrackedEvent> InterestingEvents { get; }

    private readonly ObservableCollection<TrackedEvent> interestingEvents = new();

    public int MaxEventsDisplayed
    {
        get => this.maxEventsDisplayed;

        set
        {
            if (value < 0)
            {
                value = 0;
            }

            this.maxEventsDisplayed = value;
            Settings.Default.MaximumTrackedEvents = value;
            this.OnPropertyChanged(nameof(this.MaxEventsDisplayed));

            if (this.maxEventsDisplayed == 0)
            {
                this.interestingEvents.Clear();
            }
            else
            {
                this.EnforceInterestingEventsLimit();
            }
        }
    }

    private void EnforceInterestingEventsLimit()
    {
        while (this.interestingEvents.Count > this.maxEventsDisplayed)
        {
            this.interestingEvents.RemoveAt(0);
        }
    }

    public ICollectionView AvailableEvents
    {
        get
        {
            return this.availableEvents ??= CreateCollectionViewForAvailableEvents(this.trackers);
        }
    }

    private void HandleEventHandled(object? sender, TrackedEvent trackedEvent)
    {
        if (trackedEvent.Originator.Handler is Visual visual
            && visual.IsPartOfSnoopVisualTree() == false)
        {
            Action action =
                () =>
                {
                    this.interestingEvents.Add(trackedEvent);
                    this.EnforceInterestingEventsLimit();

                    var tvi = (TreeViewItem?)this.EventTree.ItemContainerGenerator.ContainerFromItem(trackedEvent);
                    tvi?.BringIntoView();
                };

            if (this.Dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                this.RunInDispatcherAsync(action);
            }
        }
    }

    private void HandleTrackerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is EventTracker tracker
            && e.PropertyName == nameof(EventTracker.IsEnabled))
        {
            var savedTrackedEvent = Settings.Default.EventTrackers.FirstOrDefault(x => x.Id == tracker.Id);

            if (savedTrackedEvent is null)
            {
                savedTrackedEvent = new(tracker.Id);
                Settings.Default.EventTrackers.Add(savedTrackedEvent);
            }

            savedTrackedEvent.IsEnabled = tracker.IsEnabled;
        }
    }

    private void HandleClear(object sender, ExecutedRoutedEventArgs e)
    {
        this.interestingEvents.Clear();
    }

    private void HandleResetEventTrackersToDefault(object sender, ExecutedRoutedEventArgs e)
    {
        Settings.Default.EventTrackers.Clear();

        foreach (var eventTracker in this.trackers)
        {
            eventTracker.IsEnabled = defaultEvents.Contains(eventTracker.RoutedEvent);
        }
    }

    private void EventTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not null)
        {
            if (e.NewValue is EventEntry entry)
            {
                SnoopUI.InspectCommand.Execute(entry.Handler, this);
            }
            else if (e.NewValue is TrackedEvent @event)
            {
                SnoopUI.InspectCommand.Execute(@event.EventArgs, this);
            }
        }
    }

    private readonly ObservableCollection<EventTracker> trackers = new();

    private static readonly List<RoutedEvent> defaultEvents =
        new(
            new[]
            {
                Keyboard.KeyDownEvent,
                Keyboard.KeyUpEvent,
                TextCompositionManager.TextInputEvent,
                Mouse.MouseDownEvent,
                Mouse.PreviewMouseDownEvent,
                Mouse.MouseUpEvent,
                CommandManager.ExecutedEvent,
            });

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    private static ICollectionView CreateCollectionViewForAvailableEvents(ObservableCollection<EventTracker> trackers)
    {
        var pgd = new PropertyGroupDescription
        {
            PropertyName = nameof(EventTracker.Category),
            StringComparison = StringComparison.OrdinalIgnoreCase
        };

        var cvs = new CollectionViewSource();
        cvs.SortDescriptions.Add(new SortDescription(nameof(EventTracker.Category), ListSortDirection.Ascending));
        cvs.SortDescriptions.Add(new SortDescription(nameof(EventTracker.Name), ListSortDirection.Ascending));
        cvs.GroupDescriptions.Add(pgd);

        cvs.Source = trackers;

        cvs.View.Refresh();
        return cvs.View;
    }

    public void Dispose()
    {
        foreach (var tracker in this.trackers)
        {
            tracker.Dispose();
        }
    }
}