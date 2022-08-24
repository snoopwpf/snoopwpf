namespace Snoop.Controls;

using System.Windows.Documents;
using System.Windows.Input;

public class NoFocusHyperlink : Hyperlink
{
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        this.OnClick();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}