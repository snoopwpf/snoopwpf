using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Snoop
{
	public static class SnoopWindowUtils
	{
		public static Window FindOwnerWindow()
		{
			Window ownerWindow = null;

			if (Application.Current != null)
			{
				if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
				{
					// first: set the owner window as the current application's main window, if visible.
					ownerWindow = Application.Current.MainWindow;
				}
				else
				{
					// second: try and find a visible window in the list of the current application's windows
					foreach (Window window in Application.Current.Windows)
					{
						if (window.Visibility == Visibility.Visible)
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
					if
					(
						presentationSource.RootVisual is Window &&
						((Window)presentationSource.RootVisual).Visibility == Visibility.Visible
					)
					{
						ownerWindow = (Window)presentationSource.RootVisual;
						break;
					}
				}
			}

			return ownerWindow;
		}
	}
}
