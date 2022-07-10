namespace Snoop;

using System.Windows;

public partial class WindowInfoControl
{
    public WindowInfoControl()
    {
        this.InitializeComponent();

        this.DataContextChanged += this.WindowInfoControl_DataContextChanged;
    }

    private void WindowInfoControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        this.WindowInfoContainer.Visibility = e.NewValue is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}