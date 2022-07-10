namespace Snoop.Views.TriggersTab.Triggers;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Snoop.Infrastructure.Helpers;

public class MultiTriggerItem : TriggerItemBase
{
    private readonly MultiTrigger trigger;
    private readonly DependencyObject source;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MultiTriggerItem" /> class.
    /// </summary>
    public MultiTriggerItem(MultiTrigger trigger, DependencyObject source, TriggerSource triggerSource)
        : base(trigger, source, TriggerType.MultiTrigger, triggerSource)
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
            var instance = this.Instance;

            // todo: why did we need this?
            if (condition.SourceName is not null
                && this.TriggerSource == TriggerSource.ControlTemplate
                && this.Instance is Control control
                && control.Template is not null)
            {
                if (control.Template.FindName(condition.SourceName, control) is DependencyObject source)
                {
                    instance = source;
                }
            }

            var realInstance = TemplateHelper.GetChildFromTemplateIfNeeded(this.source, condition.SourceName) as DependencyObject;

            if (realInstance is not null)
            {
                yield return new ConditionItem(condition.Property, realInstance, condition.Value)
                {
                    SourceName = condition.SourceName
                };
            }
        }
    }
}