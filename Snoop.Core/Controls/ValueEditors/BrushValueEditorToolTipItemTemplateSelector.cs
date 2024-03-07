namespace Snoop.Controls.ValueEditors;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Snoop.Converters;

public class BrushValueEditorToolTipItemTemplateSelector : DataTemplateSelector
{
    public static readonly BrushValueEditorToolTipItemTemplateSelector Instance = new();

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            SolidColorBrush or BrushStop => (DataTemplate)((FrameworkElement)container).FindResource("Snoop.DataTemplates.ToolTip.BrushStop"),
            _ => (DataTemplate)((FrameworkElement)container).FindResource("Snoop.DataTemplates.ToolTip.Brush")
        };
    }
}