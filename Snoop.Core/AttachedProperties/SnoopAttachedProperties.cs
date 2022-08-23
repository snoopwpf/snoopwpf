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
}