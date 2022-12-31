namespace Snoop.Views.TriggersTab.Triggers;

using System.Windows;
using System.Xml.Linq;
using Snoop.Infrastructure.Helpers;

public class TriggerActionItem
{
    public TriggerActionItem(TriggerAction triggerAction, DependencyObject source)
    {
        this.Source = source;
        this.TriggerAction = triggerAction;
    }

    public DependencyObject Source { get; }

    public TriggerAction TriggerAction { get; }

    public string? DescriptiveValue { get; protected set; }

    public object? ToolTip { get; protected set; }

    public TriggerSource TriggerSource { get; set; }

    public virtual void Initialize()
    {
        var xaml = XamlWriterHelper.GetXamlAsXElement(this.TriggerAction).RemoveNamespaces();

        this.DescriptiveValue = xaml.ToString(SaveOptions.DisableFormatting);
        this.ToolTip = xaml.ToString(SaveOptions.None);
    }
}