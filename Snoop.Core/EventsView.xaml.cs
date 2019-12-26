// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using JetBrains.Annotations;
    using Snoop.Infrastructure;

    public partial class EventsView : INotifyPropertyChanged
    {
        public static readonly RoutedCommand ClearCommand = new RoutedCommand();

        public EventsView()
        {
            this.InitializeComponent();

            var sorter = new List<EventTracker>();

            foreach (var routedEvent in EventManager.GetRoutedEvents())
            {
                var tracker = new EventTracker(typeof(UIElement), routedEvent);
                tracker.EventHandled += this.HandleEventHandled;
                sorter.Add(tracker);

                if (defaultEvents.Contains(routedEvent))
                {
                    tracker.IsEnabled = true;
                }
            }

            sorter.Sort();
            foreach (var tracker in sorter)
            {
                this.trackers.Add(tracker);
            }

            this.CommandBindings.Add(new CommandBinding(ClearCommand, this.HandleClear));
        }

        public IEnumerable InterestingEvents
        {
            get { return this.interestingEvents; }
        }

        private readonly ObservableCollection<TrackedEvent> interestingEvents = new ObservableCollection<TrackedEvent>();

        public object AvailableEvents
        {
            get
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

                cvs.Source = this.trackers;

                cvs.View.Refresh();
                return cvs.View;
            }
        }

        private void HandleEventHandled(TrackedEvent trackedEvent)
        {
            var visual = trackedEvent.Originator.Handler as Visual;
            if (visual != null && !visual.IsPartOfSnoopVisualTree())
            {
                Action action =
                    () =>
                    {
                        this.interestingEvents.Add(trackedEvent);

                        while (this.interestingEvents.Count > 100)
                        {
                            this.interestingEvents.RemoveAt(0);
                        }

                        var tvi = (TreeViewItem)this.EventTree.ItemContainerGenerator.ContainerFromItem(trackedEvent);
                        if (tvi != null)
                        {
                            tvi.BringIntoView();
                        }
                    };

                if (!this.Dispatcher.CheckAccess())
                {
                    this.Dispatcher.BeginInvoke(action);
                }
                else
                {
                    action.Invoke();
                }
            }
        }

        private void HandleClear(object sender, ExecutedRoutedEventArgs e)
        {
            this.interestingEvents.Clear();
        }

        private void EventTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                if (e.NewValue is EventEntry)
                {
                    SnoopUI.InspectCommand.Execute(((EventEntry)e.NewValue).Handler, this);
                }
                else if (e.NewValue is TrackedEvent)
                {
                    SnoopUI.InspectCommand.Execute(((TrackedEvent)e.NewValue).EventArgs, this);
                }
            }
        }

        private readonly ObservableCollection<EventTracker> trackers = new ObservableCollection<EventTracker>();

        private static readonly List<RoutedEvent> defaultEvents =
            new List<RoutedEvent>(
                new RoutedEvent[]
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
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class InterestingEvent
    {
        public InterestingEvent(object handledBy, RoutedEventArgs eventArgs)
        {
            this.handledBy = handledBy;
            this.triggeredOn = null;
            this.eventArgs = eventArgs;
        }

        public RoutedEventArgs EventArgs
        {
            get { return this.eventArgs; }
        }

        private readonly RoutedEventArgs eventArgs;

        public object HandledBy
        {
            get { return this.handledBy; }
        }

        private readonly object handledBy;

        public object TriggeredOn
        {
            get { return this.triggeredOn; }
        }

        private readonly object triggeredOn;

        public bool Handled
        {
            get { return this.handledBy != null; }
        }
    }
}
