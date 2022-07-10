namespace Snoop.Views.TriggersTab.Triggers;

using System.Windows;

public static class TriggerItemFactory
{
    public static TriggerItemBase? GetTriggerItem(TriggerBase trigger, DependencyObject source, TriggerSource triggerSource)
    {
        TriggerItemBase triggerItem;
        if (trigger is Trigger)
        {
            triggerItem = new TriggerItem((Trigger)trigger, source, triggerSource);
        }
        else if (trigger is DataTrigger)
        {
            triggerItem = new DataTriggerItem((DataTrigger)trigger, source, triggerSource);
        }
        else if (trigger is MultiTrigger)
        {
            triggerItem = new MultiTriggerItem((MultiTrigger)trigger, source, triggerSource);
        }
        else if (trigger is MultiDataTrigger)
        {
            triggerItem = new MultiDataTriggerItem((MultiDataTrigger)trigger, source, triggerSource);
        }
        else if (trigger is EventTrigger)
        {
            triggerItem = new EventTriggerItem((EventTrigger)trigger, source, triggerSource);
        }
        else
        {
            return null;
        }

        triggerItem.Initialize();
        return triggerItem;
    }
}