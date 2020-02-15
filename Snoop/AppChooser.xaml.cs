// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Snoop.Infrastructure;
    using Snoop.Properties;
    using Snoop.Views;
    using Snoop.Windows;

    public partial class AppChooser
    {
        public static readonly RoutedCommand InspectCommand = new RoutedCommand(nameof(InspectCommand), typeof(AppChooser));
        public static readonly RoutedCommand RefreshCommand = new RoutedCommand(nameof(RefreshCommand), typeof(AppChooser));
        public static readonly RoutedCommand MagnifyCommand = new RoutedCommand(nameof(MagnifyCommand), typeof(AppChooser));
        public static readonly RoutedCommand SettingsCommand = new RoutedCommand(nameof(SettingsCommand), typeof(AppChooser));
        public static readonly RoutedCommand MinimizeCommand = new RoutedCommand(nameof(MinimizeCommand), typeof(AppChooser));

        private readonly ObservableCollection<WindowInfo> windowInfos = new ObservableCollection<WindowInfo>();

        static AppChooser()
        {
            RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
        }

        public AppChooser()
        {
            this.WindowInfos = CollectionViewSource.GetDefaultView(this.windowInfos);

            this.InitializeComponent();

            this.CommandBindings.Add(new CommandBinding(RefreshCommand, this.HandleRefreshCommand));
            this.CommandBindings.Add(new CommandBinding(InspectCommand, this.HandleInspectCommand, this.HandleCanInspectOrMagnifyCommand));
            this.CommandBindings.Add(new CommandBinding(MagnifyCommand, this.HandleMagnifyCommand, this.HandleCanInspectOrMagnifyCommand));
            this.CommandBindings.Add(new CommandBinding(SettingsCommand, this.HandleSettingsCommand));
            this.CommandBindings.Add(new CommandBinding(MinimizeCommand, this.HandleMinimizeCommand));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, this.HandleCloseCommand));
        }

        public ICollectionView WindowInfos { get; }

        public void Refresh()
        {
            this.windowInfos.Clear();

            this.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                (DispatcherOperationCallback)(x =>
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;

                        var processes = Process.GetProcesses().Except(new[] { Process.GetCurrentProcess() });

                        foreach (var process in processes)
                        {
                            var windows = NativeMethods.GetRootWindowsOfProcess(process.Id);
                            var windowInfoCollection = windows.Select(h => new WindowInfo(h, process));
                            foreach (var windowInfo in windowInfoCollection)
                            {
                                if (windowInfo.IsValidProcess && !this.IsAlreadInList(process))
                                {
                                    this.windowInfos.Add(windowInfo);
                                }
                            }
                        }

                        if (this.windowInfos.Count > 0)
                        {
                            this.WindowInfos.MoveCurrentTo(this.windowInfos[0]);
                        }
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }

                    return null;
                }),
                null);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // load the window placement details from the user settings.
            SnoopWindowUtils.LoadWindowPlacement(this, Settings.Default.AppChooserWindowPlacement);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // persist the window placement details to the user settings.
            SnoopWindowUtils.SaveWindowPlacement(this, wp => Settings.Default.AppChooserWindowPlacement = wp);

            Settings.Default.Save();
        }

        private bool IsAlreadInList(Process process)
        {
            foreach (var window in this.windowInfos)
            {
                if (window.OwningProcessInfo.Process.Id == process.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleCanInspectOrMagnifyCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.WindowInfos.CurrentItem != null)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
        }

        private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (WindowInfo)this.WindowInfos.CurrentItem;
            var result = window?.OwningProcessInfo.Snoop(window.HWnd);

            if (result?.Success == false)
            {
                ErrorDialog.ShowDialog(result.AttachException, "Can't Snoop the process", $"Failed to attach to '{result.WindowName}'.", true);
            }
        }

        private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (WindowInfo)this.WindowInfos.CurrentItem;
            var result = window.OwningProcessInfo?.Magnify(window.HWnd);

            if (result?.Success == false)
            {
                ErrorDialog.ShowDialog(result.AttachException, "Can't Snoop the process", $"Failed to attach to '{result.WindowName}'.", true);
            }
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

        private void HandleWindowsComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            if (this.windowInfos.Any())
            {
                return;
            }

            RefreshCommand.Execute(null, this);
        }
    }

    public class AttachResult
    {
        public AttachResult()
        {
            this.Success = true;
        }

        public AttachResult(Exception attachException)
        {
            this.Success = false;

            this.AttachException = attachException;
        }

        public AttachResult(Exception attachException, string windowName)
        {
            this.Success = false;

            this.AttachException = attachException;
            this.WindowName = windowName;
        }

        public bool Success { get; }

        public Exception AttachException { get; }

        public string WindowName { get; }
    }
}