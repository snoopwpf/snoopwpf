// ReSharper disable once CheckNamespace
namespace Snoop;

using System.Windows;

public static class WindowExtensions
{
    public static bool? ShowDialogEx(this Window window, DependencyObject dependencyObject)
    {
        var ownerWindow = Window.GetWindow(dependencyObject);

        if (ownerWindow is not null
            && ReferenceEquals(ownerWindow, window) == false)
        {
            window.Owner = ownerWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        return window.ShowDialog();
    }
}