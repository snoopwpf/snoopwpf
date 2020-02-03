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
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Class that shows all the routed events occurring on a visual.
    /// VERY dangerous (cannot unregister for the events) and doesn't work all that great.
    /// Stay far away from this code :)
    /// </summary>
    public class EventsListener
    {
        private static EventsListener current;
        private readonly Visual visual;

        private static readonly Dictionary<Type, Type> registeredTypes = new Dictionary<Type, Type>();

        public EventsListener(Visual visual)
        {
            current = this;
            this.visual = visual;

            var type = visual.GetType();

            // Cannot unregister for events once we've registered, so keep the registration simple and only do it once.
            for (var baseType = type; baseType != null; baseType = baseType.BaseType)
            {
                if (registeredTypes.ContainsKey(baseType))
                {
                    continue;
                }

                registeredTypes[baseType] = baseType;

                var routedEvents = EventManager.GetRoutedEventsForOwner(baseType);
                if (routedEvents != null)
                {
                    foreach (var routedEvent in routedEvents)
                    {
                        EventManager.RegisterClassHandler(baseType, routedEvent, new RoutedEventHandler(HandleEvent), true);
                    }
                }
            }
        }

        public ObservableCollection<EventInformation> Events { get; } = new ObservableCollection<EventInformation>();

        public static string Filter { get; set; }

        public static void Stop()
        {
            current = null;
        }

        private static void HandleEvent(object sender, RoutedEventArgs e)
        {
            if (current == null
                || ReferenceEquals(sender, current.visual) == false)
            {
                return;
            }

            if (string.IsNullOrEmpty(Filter)
                || e.RoutedEvent.Name.ContainsIgnoreCase(Filter))
            {
                current.Events.Add(new EventInformation(e));

                while (current.Events.Count > 100)
                {
                    current.Events.RemoveAt(0);
                }
            }
        }
    }

    public class EventInformation
    {
        public EventInformation(RoutedEventArgs evt)
        {
            this.evt = evt;
        }

        public IEnumerable Properties => PropertyInformation.GetProperties(this.evt);

        public override string ToString()
        {
            return $"{this.evt.RoutedEvent.Name} Handled: {this.evt.Handled} OriginalSource: {this.evt.OriginalSource}";
        }

        private readonly RoutedEventArgs evt;
    }
}
