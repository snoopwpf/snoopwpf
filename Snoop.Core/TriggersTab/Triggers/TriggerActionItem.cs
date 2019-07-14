namespace Snoop.TriggersTab.Triggers
{
    using System.Windows;
    using System.Windows.Markup;
    using System.Xml.Linq;

    public class TriggerActionItem
    {
        public TriggerActionItem(TriggerAction triggerAction, DependencyObject source)
        {
            this.Source = source;
            this.TriggerAction = triggerAction;
        }

        public DependencyObject Source { get; private set; }

        public TriggerAction TriggerAction { get; private set; }

        public string DescriptiveValue { get; protected set; }

        public object ToolTip { get; protected set; }

        public virtual void Initialize()
        {
            var xaml = BindingDisplayHelper.RemoveNamespacesFromXml(XamlWriter.Save(this.TriggerAction));

            this.DescriptiveValue = xaml.ToString(SaveOptions.DisableFormatting);
            this.ToolTip = xaml.ToString(SaveOptions.None);
        }
    }
}