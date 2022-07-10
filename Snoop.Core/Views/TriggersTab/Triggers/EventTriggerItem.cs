namespace Snoop.Views.TriggersTab.Triggers;

using System.Collections.Generic;
using System.Linq;
using System.Windows;

public class EventTriggerItem : TriggerItemBase
{
    private readonly EventTrigger eventTrigger;

    public EventTriggerItem(EventTrigger eventTrigger, DependencyObject source, TriggerSource triggerSource)
        : base(eventTrigger, source, TriggerType.EventTrigger, triggerSource)
    {
        this.eventTrigger = eventTrigger;
    }

    private string GetDisplayName()
    {
        return string.Format("SourceName: {0}, Event: {1}", this.eventTrigger.SourceName, this.eventTrigger.RoutedEvent.Name);
    }

    /// <inheritdoc />
    protected override IEnumerable<ConditionItem> GetConditions()
    {
        yield return new ConditionItem(this.Instance, null, this.GetDisplayName());
    }

    /// <inheritdoc />
    protected override IEnumerable<SetterItem> GetSetters()
    {
        return Enumerable.Empty<SetterItem>();
    }

    /// <inheritdoc />
    protected override IEnumerable<TriggerActionItem> GetEnterActions()
    {
        return this.eventTrigger.Actions.Select(x => TriggerActionItemFactory.GetTriggerActionItem(x, this.Instance, this.TriggerSource))
            .Concat(base.GetEnterActions());
    }
}