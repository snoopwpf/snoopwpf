namespace Snoop {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Windows;
	using System.Windows.Media;

	/// <summary>
	/// Class that shows all the routed events occurring on a visual.
	/// VERY dangerous (cannot unregister for the events) and doesn't work all that great.
	/// Stay far away from this code :)
	/// </summary>
	public class EventsListener
	{
		private static EventsListener current = null;
		private Visual visual;
		private static Dictionary<Type, Type> registeredTypes = new Dictionary<Type, Type>();
		public static string filter = null;

		private ObservableCollection<EventInformation> events = new ObservableCollection<EventInformation>();

		public EventsListener(Visual visual) {
			EventsListener.current = this;
			this.visual = visual;

			Type type = visual.GetType();

		
			// Cannot unregister for events once we've registered, so keep the registration simple and only do it once.
			for (Type baseType = type; baseType != null; baseType = baseType.BaseType) {
				if (!registeredTypes.ContainsKey(baseType)) {
					registeredTypes[baseType] = baseType;

					RoutedEvent[] routedEvents = EventManager.GetRoutedEventsForOwner(baseType);
					if (routedEvents != null) {
						foreach (RoutedEvent routedEvent in routedEvents)
							EventManager.RegisterClassHandler(baseType, routedEvent, new RoutedEventHandler(EventsListener.HandleEvent), true);
					}
				}
			}
		}

		public static void Stop() {
			EventsListener.current = null;
		}

		public static string Filter {
			get { return EventsListener.filter; }
			set {
				EventsListener.filter = value;
				if (EventsListener.filter != null)
					EventsListener.filter = EventsListener.filter.ToLower();
			}
		}

		private static void HandleEvent(object sender, RoutedEventArgs e) {
			if (EventsListener.current != null && sender == EventsListener.current.visual) {
				if (string.IsNullOrEmpty(EventsListener.Filter) || e.RoutedEvent.Name.ToLower().Contains(EventsListener.Filter)) {
					EventsListener.current.events.Add(new EventInformation(e));

					while (EventsListener.current.events.Count > 100)
						EventsListener.current.events.RemoveAt(0);
				}
			}
		}

		public ObservableCollection<EventInformation> Events {
			get { return this.events; }
		}
	}

	public class EventInformation {
		private RoutedEventArgs evt;

		public EventInformation(RoutedEventArgs evt) {
			this.evt = evt;
		}

		public IEnumerable Properties {
			get { return PropertyInformation.GetProperties(this.evt); }
		}

		public override string ToString() {
			return string.Format("{0} Handled: {1} OriginalSource: {2}", evt.RoutedEvent.Name, evt.Handled, evt.OriginalSource);
		}
	}
}
