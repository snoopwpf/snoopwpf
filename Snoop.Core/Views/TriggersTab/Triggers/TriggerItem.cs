namespace Snoop.Views.TriggersTab.Triggers;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Snoop.Infrastructure.Helpers;

public class TriggerItem : TriggerItemBase
{
    private readonly DependencyObject source;
    private readonly Trigger trigger;

    public TriggerItem(Trigger trigger, DependencyObject source, TriggerSource triggerSource)
        : base(trigger, source, TriggerType.Trigger, triggerSource)
    {
        this.trigger = trigger;
        this.source = source;
    }

    protected override IEnumerable<SetterItem> GetSetters()
    {
        return this.trigger.Setters.Select(s => new SetterItem(s, this.source));
    }

    protected override IEnumerable<ConditionItem> GetConditions()
    {
        var realInstance = TemplateHelper.GetChildFromTemplateIfNeeded(this.source, this.trigger.SourceName) as DependencyObject;

        if (realInstance is not null)
        {
            yield return new ConditionItem(this.trigger.Property, realInstance, this.trigger.Value)
            {
                SourceName = this.trigger.SourceName
            };
        }
    }
}