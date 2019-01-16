// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Snoop
{
    using System.Windows;
    using Snoop.Properties;
    using Snoop.Views;

    public partial class AppChooser
	{
		static AppChooser()
		{
			RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
		}

		public AppChooser()
		{
			this.Windows = CollectionViewSource.GetDefaultView(this.windows);

			this.InitializeComponent();

			this.CommandBindings.Add(new CommandBinding(RefreshCommand, this.HandleRefreshCommand));
			this.CommandBindings.Add(new CommandBinding(InspectCommand, this.HandleInspectCommand, this.HandleCanInspectOrMagnifyCommand));
			this.CommandBindings.Add(new CommandBinding(MagnifyCommand, this.HandleMagnifyCommand, this.HandleCanInspectOrMagnifyCommand));
		    this.CommandBindings.Add(new CommandBinding(SettingsCommand, this.HandleSettingsCommand));
            this.CommandBindings.Add(new CommandBinding(MinimizeCommand, this.HandleMinimizeCommand));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, this.HandleCloseCommand));
		}

		public static readonly RoutedCommand InspectCommand = new RoutedCommand();
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand();
		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand();
	    public static readonly RoutedCommand SettingsCommand = new RoutedCommand();
        public static readonly RoutedCommand MinimizeCommand = new RoutedCommand();

        public ICollectionView Windows { get; }

        private readonly ObservableCollection<WindowInfo> windows = new ObservableCollection<WindowInfo>();

		public void Refresh()
		{
			this.windows.Clear();

		    this.Dispatcher.BeginInvoke
			(
				DispatcherPriority.Loaded,
				(DispatcherOperationCallback)delegate
				{
					try
					{
						Mouse.OverrideCursor = Cursors.Wait;

						foreach (var windowHandle in NativeMethods.ToplevelWindows)
						{
							var window = new WindowInfo(windowHandle);
							if (window.IsValidProcess 
							    && !this.HasProcess(window.OwningProcess))
							{
								new AttachFailedHandler(window, this);
								this.windows.Add(window);
							}
						}

						if (this.windows.Count > 0)
                        {
                            this.Windows.MoveCurrentTo(this.windows[0]);
                        }
                    }
					finally
					{
						Mouse.OverrideCursor = null;
					}
					return null;
				},
				null
			);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

		    // load the window placement details from the user settings.
		    SnoopWindowUtils.LoadWindowPlacement(this, Properties.Settings.Default.AppChooserWindowPlacement);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

		    // persist the window placement details to the user settings.
            SnoopWindowUtils.SaveWindowPlacement(this, wp => Properties.Settings.Default.AppChooserWindowPlacement = wp);
		
			Properties.Settings.Default.Save();
		}

		private bool HasProcess(Process process)
		{
			foreach (var window in this.windows)
            {
                if (window.OwningProcess.Id == process.Id)
                {
                    return true;
                }
            }

            return false;
		}

		private void HandleCanInspectOrMagnifyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (this.Windows.CurrentItem != null)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
		}

		private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var window = (WindowInfo)this.Windows.CurrentItem;
		    window?.Snoop();
		}

		private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var window = (WindowInfo)this.Windows.CurrentItem;
		    window?.Magnify();
		}

		private void HandleRefreshCommand(object sender, ExecutedRoutedEventArgs e)
		{
			// clear out cached process info to make the force refresh do the process check over again.
			WindowInfo.ClearCachedWindowHandleInfo();
			this.Refresh();
		}

	    private void HandleSettingsCommand(object sender, ExecutedRoutedEventArgs e)
	    {
	        var window = new Window
	                     {
	                         Content = new SettingsView(),
	                         Title = "Settings for snoop",
                             Owner = this,
	                         MinWidth = 480,
	                         MinHeight = 320,
                             Width = 480,
                             Height = 320,
                             WindowStartupLocation = WindowStartupLocation.CenterScreen,
                             WindowStyle = WindowStyle.ToolWindow
	                     };
	        window.ShowDialog();
            // Reload here to require users to explicitly save the settings from the dialog. Reload just discards any unsaved changes.
            Settings.Default.Reload();
	    }        

        private void HandleMinimizeCommand(object sender, ExecutedRoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private void HandleCloseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.DragMove();
		}
	}

	public class AttachFailedEventArgs : EventArgs
	{
		public AttachFailedEventArgs(Exception attachException, string windowName)
		{
		    this.AttachException = attachException;
		    this.WindowName = windowName;
		}

	    public Exception AttachException { get; }

	    public string WindowName { get; }
	}

	public class AttachFailedHandler
	{
		public AttachFailedHandler(WindowInfo window, AppChooser appChooser = null)
		{
			window.AttachFailed += this.OnSnoopAttachFailed;
			this.appChooser = appChooser;
		}

		private void OnSnoopAttachFailed(object sender, AttachFailedEventArgs e)
		{
            ErrorDialog.ShowDialog(e.AttachException, "Can't Snoop the process", $"Failed to attach to '{e.WindowName}'.", exceptionAlreadyHandled: true);
	        
		    // TODO This should be implemented through the event broker, not like this.
		    this.appChooser?.Refresh();
		}

		private readonly AppChooser appChooser;
	}
}