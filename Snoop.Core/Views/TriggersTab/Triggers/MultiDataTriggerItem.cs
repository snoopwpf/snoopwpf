namespace Snoop.Views.TriggersTab.Triggers;

using System.Collections.Generic;
using System.Linq;
using System.Windows;

public class MultiDataTriggerItem : TriggerItemBase
{
    private readonly DependencyObject source;
    private readonly MultiDataTrigger trigger;

    public MultiDataTriggerItem(MultiDataTrigger trigger, DependencyObject source, TriggerSource triggerSource)
        : base(trigger, source, TriggerType.MultiDataTrigger, triggerSource)
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
        foreach (var condition in this.trigger.Conditions)
        {
            yield return new ConditionItem(condition.Binding, this.Instance, condition.Value);
        }
    }
}