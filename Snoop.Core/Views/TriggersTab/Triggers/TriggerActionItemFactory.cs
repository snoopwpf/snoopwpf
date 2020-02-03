namespace Snoop.TriggersTab.Triggers
{
    using System.Windows;

    public static class TriggerActionItemFactory
    {
        public static TriggerActionItem GetTriggerActionItem(TriggerAction triggerAction, DependencyObject source, TriggerSource triggerSource)
        {
            TriggerActionItem triggerActionItem;

            triggerActionItem = new TriggerActionItem(triggerAction, source);

            triggerActionItem.Initialize();

            return triggerActionItem;
        }
    }
}