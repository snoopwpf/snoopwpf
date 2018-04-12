namespace Snoop.TriggersTab.Triggers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using Snoop.Infrastructure;

    public class MultiTriggerItem : TriggerItemBase
    {
        private readonly MultiTrigger trigger;
        private readonly FrameworkElement source;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultiTriggerItem" /> class.
        /// </summary>
        public MultiTriggerItem(MultiTrigger trigger, FrameworkElement source, TriggerSource triggerSource)
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
                var control = this.Instance as Control;

                if (condition.SourceName != null
                    && this.TriggerSource == TriggerSource.ControlTemplate
                    && control != null
                    && control.Template != null)
                {
                    var source = control.Template.FindName(condition.SourceName, control) as DependencyObject;
                    if (source != null)
                    {
                        instance = source;
                    }
                }

                var realInstance = TemplateHelper.GetChildFromTemplateIfNeeded(this.source, condition.SourceName) as DependencyObject;

                yield return new ConditionItem(condition.Property, realInstance, condition.Value)
                    {
                        SourceName = condition.SourceName
                    };
            }
        }
    }
}