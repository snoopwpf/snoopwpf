namespace Snoop {
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

	public partial class EventsView: INotifyPropertyChanged
	{
		public static readonly DependencyProperty ScopeProperty = DependencyProperty.Register("Scope", typeof(Visual), typeof(EventsView));

		public static readonly RoutedCommand ClearCommand = new RoutedCommand();

		private ObservableCollection<TrackedEvent> interestingEvents = new ObservableCollection<TrackedEvent>();
		private ObservableCollection<EventTracker> trackers = new ObservableCollection<EventTracker>();

		private static List<RoutedEvent> defaultEvents = new List<RoutedEvent>(new RoutedEvent[] {
			Keyboard.KeyDownEvent,
			Keyboard.KeyUpEvent,
			TextCompositionManager.TextInputEvent,
			Mouse.MouseDownEvent,
			Mouse.PreviewMouseDownEvent,
			Mouse.MouseUpEvent,
			CommandManager.ExecutedEvent,
			});

		public EventsView() {
			this.InitializeComponent();

			List<EventTracker> sorter = new List<EventTracker>();

			foreach (RoutedEvent routedEvent in EventManager.GetRoutedEvents()) {
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

		public Visual Scope {
			get { return (Visual)this.GetValue(EventsView.ScopeProperty); }
			set { this.SetValue(EventsView.ScopeProperty, value); }
		}

		public IEnumerable InterestingEvents {
			get { return this.interestingEvents; }
		}

		public object AvailableEvents {
			get {
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
			Visual scope = this.Scope;
			if (visual != null && scope != null) {
				if (visual.IsDescendantOf(scope)) {
					this.interestingEvents.Add(trackedEvent);

					while (this.interestingEvents.Count > 100)
						this.interestingEvents.RemoveAt(0);

					TreeViewItem tvi = (TreeViewItem)this.EventTree.ItemContainerGenerator.ContainerFromItem(trackedEvent);
					if (tvi != null)
						tvi.BringIntoView();
				}
			}
		}

		private void HandleClear(object sender, ExecutedRoutedEventArgs e) {
			this.interestingEvents.Clear();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		private void InspectHandler(object sender, EventArgs e) {
			object obj = ((FrameworkElement)sender).DataContext;
			if (obj is TrackedEvent)
				SnoopUI.InspectCommand.Execute(((TrackedEvent)obj).EventArgs, this);
			else if (obj is EventEntry)
				SnoopUI.InspectCommand.Execute(((EventEntry)obj).Handler, this);
		}
	}

	public class InterestingEvent
	{
		private object triggeredOn;
		private object handledBy;
		private RoutedEventArgs eventArgs;

		public InterestingEvent(object handledBy, RoutedEventArgs eventArgs) {
			this.handledBy = handledBy;
			this.triggeredOn = null;
			this.eventArgs = eventArgs;
		}

		public RoutedEventArgs EventArgs {
			get { return this.eventArgs; }
		}

		public object HandledBy {
			get { return this.handledBy; }
		}

		public object TriggeredOn {
			get { return this.triggeredOn; }
		}

		public bool Handled {
			get { return this.handledBy != null; }
		}
	}
}
