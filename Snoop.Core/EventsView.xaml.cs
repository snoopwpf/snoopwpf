// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using Snoop.Infrastructure;

namespace Snoop
{
	public partial class EventsView : INotifyPropertyChanged
	{
		public static readonly RoutedCommand ClearCommand = new RoutedCommand();


		public EventsView()
		{
			this.InitializeComponent();

			List<EventTracker> sorter = new List<EventTracker>();

			foreach (RoutedEvent routedEvent in EventManager.GetRoutedEvents())
			{
				EventTracker tracker = new EventTracker(typeof(UIElement), routedEvent);
				tracker.EventHandled += this.HandleEventHandled;
				sorter.Add(tracker);

				if (EventsView.defaultEvents.Contains(routedEvent))
					tracker.IsEnabled = true;
			}

			sorter.Sort();
			foreach (EventTracker tracker in sorter)
				this.trackers.Add(tracker);

			this.CommandBindings.Add(new CommandBinding(EventsView.ClearCommand, this.HandleClear));
		}


		public IEnumerable InterestingEvents
		{
			get { return this.interestingEvents; }
		}
		private ObservableCollection<TrackedEvent> interestingEvents = new ObservableCollection<TrackedEvent>();

		public object AvailableEvents
		{
			get
			{
				PropertyGroupDescription pgd = new PropertyGroupDescription();
				pgd.PropertyName = "Category";
				pgd.StringComparison = StringComparison.OrdinalIgnoreCase;

				CollectionViewSource cvs = new CollectionViewSource();
				cvs.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
				cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
				cvs.GroupDescriptions.Add(pgd);

				cvs.Source = this.trackers;

				cvs.View.Refresh();
				return cvs.View;
			}
		}


		private void HandleEventHandled(TrackedEvent trackedEvent)
		{
			Visual visual = trackedEvent.Originator.Handler as Visual;
			if (visual != null && !visual.IsPartOfSnoopVisualTree())
			{
				Action action =
					() =>
					{
						this.interestingEvents.Add(trackedEvent);

						while (this.interestingEvents.Count > 100)
							this.interestingEvents.RemoveAt(0);

						TreeViewItem tvi = (TreeViewItem)this.EventTree.ItemContainerGenerator.ContainerFromItem(trackedEvent);
						if (tvi != null)
							tvi.BringIntoView();
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
					SnoopUI.InspectCommand.Execute(((EventEntry)e.NewValue).Handler, this);
				else if (e.NewValue is TrackedEvent)
					SnoopUI.InspectCommand.Execute(((TrackedEvent)e.NewValue).EventArgs, this);
			}
		}


		private ObservableCollection<EventTracker> trackers = new ObservableCollection<EventTracker>();


		private static List<RoutedEvent> defaultEvents =
			new List<RoutedEvent>
			(
				new RoutedEvent[]
				{
					Keyboard.KeyDownEvent,
					Keyboard.KeyUpEvent,
					TextCompositionManager.TextInputEvent,
					Mouse.MouseDownEvent,
					Mouse.PreviewMouseDownEvent,
					Mouse.MouseUpEvent,
					CommandManager.ExecutedEvent,
				}
			);


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
		private RoutedEventArgs eventArgs;


		public object HandledBy
		{
			get { return this.handledBy; }
		}
		private object handledBy;


		public object TriggeredOn
		{
			get { return this.triggeredOn; }
		}
		private object triggeredOn;


		public bool Handled
		{
			get { return this.handledBy != null; }
		}
	}
}
