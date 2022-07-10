namespace Snoop.Views.DebugListenerTab;

using System;
using System.Collections.Generic;
using System.Diagnostics;

public sealed class SnoopDebugListener : TraceListener
{
    private readonly IList<IListener> listeners = new List<IListener>();

    public void RegisterListener(IListener listener)
    {
        this.listeners.Add(listener);
    }

    public const string ListenerName = "SnoopDebugListener";

    public SnoopDebugListener()
    {
        this.Name = ListenerName;
    }

    public override void WriteLine(string? str)
    {
        this.SendDataToListeners(str + Environment.NewLine);
    }

    public override void Write(string? str)
    {
        this.SendDataToListeners(str);
    }

    private void SendDataToListeners(string? str)
    {
        foreach (var listener in this.listeners)
        {
            listener.Write(str);
        }
    }

    public override void Write(string? message, string? category)
    {
        this.SendDataToListeners(message);

        base.Write(message, category);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
    {
        this.SendDataToListeners(message);
        base.TraceEvent(eventCache, source, eventType, id, message);
    }

    public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
    {
        this.SendDataToListeners(source);
        base.TraceData(eventCache, source, eventType, id, data);
    }
}