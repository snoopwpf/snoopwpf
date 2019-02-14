// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using Snoop.Infrastructure;

namespace Snoop
{
    using System.Runtime.InteropServices;
    using System.Windows.Interop;
    using Snoop.Data;
    using Rectangle = System.Drawing.Rectangle;

    public static class SnoopWindowUtils
	{
		public static Window FindOwnerWindow(Window ownedWindow)
		{
		    if (TransientSettingsData.Current.SetWindowOwner == false)
		    {
		        return null;
		    }

		    Window ownerWindow = null;

			if (SnoopModes.MultipleDispatcherMode)
			{
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
				    var window = presentationSource.RootVisual as Window;
				    if (window != null 
						&& window.Dispatcher.CheckAccess() 
						&& window.Visibility == Visibility.Visible)
					{
						ownerWindow = window;
						break;
					}
				}
			}
			else if (Application.Current != null)
			{
				if (Application.Current.MainWindow != null 
				    && Application.Current.MainWindow.CheckAccess()
				    && Application.Current.MainWindow.Visibility == Visibility.Visible)
				{
					// first: set the owner window as the current application's main window, if visible.
					ownerWindow = Application.Current.MainWindow;
				}
				else
				{
					// second: try and find a visible window in the list of the current application's windows
					foreach (Window window in Application.Current.Windows)
					{
						if (window.CheckAccess()
					        && window.Visibility == Visibility.Visible)
						{
							ownerWindow = window;
							break;
						}
					}
				}
			}

			if (ownerWindow == null)
			{
				// third: try and find a visible window in the list of current presentation sources
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
				    var window = presentationSource.RootVisual as Window;
				    if (window != null 
						&& window.CheckAccess()
						&& window.Visibility == Visibility.Visible)
					{
						ownerWindow = window;
						break;
					}
				}
			}

		    if (ReferenceEquals(ownerWindow, ownedWindow))
		    {
		        return null;
		    }

		    if (ownerWindow != null
		        && ownerWindow.Dispatcher != ownedWindow.Dispatcher)
		    {
		        return null;
		    }

		    return ownerWindow;
		}

	    public static void LoadWindowPlacement(Window window, WINDOWPLACEMENT? windowPlacement)
	    {
	        if (windowPlacement.HasValue == false
	            || IsVisibleOnAnyScreen(windowPlacement.Value.normalPosition) == false)
	        {
	            return;
	        }

	        try
	        {
	            // load the window placement details from the user settings.
	            var wp = windowPlacement.Value;
	            wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
	            wp.flags = 0;
	            wp.showCmd = (wp.showCmd == Win32.SW_SHOWMINIMIZED ? Win32.SW_SHOWNORMAL : wp.showCmd);
	            var hwnd = new WindowInteropHelper(window).Handle;
	            Win32.SetWindowPlacement(hwnd, ref wp);
	        }
	        catch
	        {
	        }
	    }

	    public static void SaveWindowPlacement(Window window, Action<WINDOWPLACEMENT> saveAction)
	    {
	        WINDOWPLACEMENT windowPlacement;
	        var hwnd = new WindowInteropHelper(window).Handle;
	        Win32.GetWindowPlacement(hwnd, out windowPlacement);

	        saveAction(windowPlacement);
	    }

	    private static bool IsVisibleOnAnyScreen(RECT rect)
	    {
	        var rectangle = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);

	        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
	        {
	            if (screen.WorkingArea.IntersectsWith(rectangle))
	            {
	                return true;
	            }
	        }

	        return false;
	    }
	}
}