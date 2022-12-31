namespace Snoop.Views.TriggersTab.Triggers;

using System.Windows;

public static class TriggerActionItemFactory
{
    public static TriggerActionItem GetTriggerActionItem(TriggerAction triggerAction, DependencyObject source, TriggerSource triggerSource)
    {
        var triggerActionItem = new TriggerActionItem(triggerAction, source)
        {
            TriggerSource = triggerSource
        };

        triggerActionItem.Initialize();

        return triggerActionItem;
    }
}