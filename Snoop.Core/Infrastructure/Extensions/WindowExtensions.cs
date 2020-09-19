namespace Snoop.Infrastructure.Extensions
{
    using System.Windows;

    public static class WindowExtensions
    {
        public static bool? ShowDialogEx(this Window window, DependencyObject dependencyObject)
        {
            var ownerWindow = Window.GetWindow(dependencyObject);

            if (ownerWindow != null
                && ReferenceEquals(ownerWindow, window) == false)
            {
                window.Owner = ownerWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            return window.ShowDialog();
        }
    }
}