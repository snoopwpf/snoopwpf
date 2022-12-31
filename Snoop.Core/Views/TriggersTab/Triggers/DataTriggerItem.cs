namespace Snoop.Views.TriggersTab.Triggers;

using System.Collections.Generic;
using System.Linq;
using System.Windows;

public class DataTriggerItem : TriggerItemBase
{
    private readonly DataTrigger dataTrigger;
    private readonly DependencyObject source;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DataTriggerItem" /> class.
    /// </summary>
    public DataTriggerItem(DataTrigger trigger, DependencyObject source, TriggerSource triggerSource)
        : base(trigger, source, TriggerType.DataTrigger, triggerSource)
    {
        this.dataTrigger = trigger;
        this.source = source;
    }

    protected override IEnumerable<SetterItem> GetSetters()
    {
        return this.dataTrigger.Setters.Select(s => new SetterItem(s, this.source));
    }

    protected override IEnumerable<ConditionItem> GetConditions()
    {
        yield return new ConditionItem(this.dataTrigger.Binding, this.Instance, this.dataTrigger.Value);
    }
}