namespace Snoop.Infrastructure;

using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Threading;

public class EventManagerWrapper
{
    public static readonly EventManagerWrapper Instance = new();

    private ConcurrentDictionary<EventRegistration, object?> EventRegistrations { get; } = new();

    public EventRegistration RegisterClassHandler(Dispatcher dispatcher, Type targetType, RoutedEvent routedEvent, RoutedEventHandler routedEventHandler, bool handledEventsToo)
    {
        var eventRegistration = new EventRegistration(dispatcher, routedEvent, routedEventHandler);
        while (this.EventRegistrations.TryAdd(eventRegistration, null) == false)
        {
        }

        EventManager.RegisterClassHandler(targetType, routedEvent, new RoutedEventHandler(this.HandleRoutedEvent), handledEventsToo);

        return eventRegistration;
    }

    public void RemoveClassHandler(EventRegistration eventRegistration)
    {
        if (this.EventRegistrations.ContainsKey(eventRegistration) == false)
        {
            return;
        }

        while (this.EventRegistrations.TryRemove(eventRegistration, out _) == false)
        {
        }
    }

    private void HandleRoutedEvent(object sender, RoutedEventArgs e)
    {
        var handlers = this.EventRegistrations;

        foreach (var item in handlers)
        {
            var handler = item.Key;

            if (handler.RoutedEvent == e.RoutedEvent
                && handler.Dispatcher == Dispatcher.CurrentDispatcher)
            {
                handler.RoutedEventHandler.Invoke(sender, e);
            }
        }
    }

    public class EventRegistration
    {
        public Dispatcher Dispatcher { get; }

        public RoutedEvent RoutedEvent { get; }

        public RoutedEventHandler RoutedEventHandler { get; }

        public EventRegistration(Dispatcher dispatcher, RoutedEvent routedEvent, RoutedEventHandler routedEventHandler)
        {
            this.Dispatcher = dispatcher;
            this.RoutedEvent = routedEvent;
            this.RoutedEventHandler = routedEventHandler;
        }
    }
}