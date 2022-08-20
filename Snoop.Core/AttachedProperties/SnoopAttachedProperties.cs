namespace Snoop.AttachedProperties;

using System.Windows;

public static class SnoopAttachedProperties
{
    public static readonly DependencyProperty IsSnoopPartProperty = DependencyProperty.RegisterAttached(
        "IsSnoopPart", typeof(bool), typeof(SnoopAttachedProperties), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetIsSnoopPart(DependencyObject element, bool value)
    {
        element.SetValue(IsSnoopPartProperty, value);
    }

    public static bool GetIsSnoopPart(DependencyObject element)
    {
        return (bool)element.GetValue(IsSnoopPartProperty);
    }

    public static readonly DependencyProperty InverterHintProperty = DependencyProperty.RegisterAttached(
        "InverterHint", typeof(InverterHint), typeof(SnoopAttachedProperties), new PropertyMetadata(default(InverterHint)));

    public static void SetInverterHint(DependencyObject element, InverterHint value)
    {
        element.SetValue(InverterHintProperty, value);
    }

    public static InverterHint GetInverterHint(DependencyObject element)
    {
        return (InverterHint)element.GetValue(InverterHintProperty);
    }
}

public enum InverterHint
{
    None,
    LeaveAsIs
}