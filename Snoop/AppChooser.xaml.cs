// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop;

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Snoop.Core;
using Snoop.Infrastructure;
using Snoop.Views;
using Snoop.Windows;

public partial class AppChooser
{
    public static readonly RoutedCommand InspectCommand = new(nameof(InspectCommand), typeof(AppChooser));
    public static readonly RoutedCommand RefreshCommand = new(nameof(RefreshCommand), typeof(AppChooser));
    public static readonly RoutedCommand MagnifyCommand = new(nameof(MagnifyCommand), typeof(AppChooser));
    public static readonly RoutedCommand SettingsCommand = new(nameof(SettingsCommand), typeof(AppChooser));
    public static readonly RoutedCommand MinimizeCommand = new(nameof(MinimizeCommand), typeof(AppChooser));

    private readonly ObservableCollection<WindowInfo> windowInfos = new();

    private LowLevelKeyboardHook? keyboardHook;

    static AppChooser()
    {
        RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
    }

    public AppChooser()
    {
        this.WindowInfos = CollectionViewSource.GetDefaultView(this.windowInfos);
        this.WindowInfos.SortDescriptions.Add(new SortDescription(nameof(WindowInfo.OwningProcessId), ListSortDirection.Ascending));
        this.SortColumn = 1;

        this.InitializeComponent();

        ThemeManager.Current.ApplyTheme(ThemeMode.Light, this);

        this.CommandBindings.Add(new CommandBinding(RefreshCommand, this.HandleRefreshCommand));
        this.CommandBindings.Add(new CommandBinding(InspectCommand, this.HandleInspectCommand, this.HandleCanInspectOrMagnifyCommand));
        this.CommandBindings.Add(new CommandBinding(MagnifyCommand, this.HandleMagnifyCommand, this.HandleCanInspectOrMagnifyCommand));
        this.CommandBindings.Add(new CommandBinding(SettingsCommand, this.HandleSettingsCommand));
        this.CommandBindings.Add(new CommandBinding(MinimizeCommand, this.HandleMinimizeCommand));
        this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, this.HandleCloseCommand));
    }

    public ICollectionView WindowInfos { get; }

    public int SortColumn { get; private set; }

    public void Refresh()
    {
        this.windowInfos.Clear();

        this.RunInDispatcherAsync(() =>
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using var currentProcess = Process.GetCurrentProcess();
                var currentProcessId = currentProcess.Id;

                var processesAndWindows = NativeMethods.GetProcessesAndWindows();

                var processToWindowInfo = new ConcurrentDictionary<int, WindowInfo>();

                foreach (var processId in processesAndWindows.Keys)
                {
                    if (processId == currentProcessId
                        || processesAndWindows.TryGetValue(processId, out var windows) == false)
                    {
                        continue;
                    }

                    var windowInfoCollection = windows.Select(h => WindowInfo.GetWindowInfo(h));

                    foreach (var windowInfo in windowInfoCollection)
                    {
                        if (processToWindowInfo.ContainsKey(processId)
                            || windowInfo.IsValidProcess == false)
                        {
                            break;
                        }

                        while (processToWindowInfo.TryAdd(processId, windowInfo) == false)
                        {
                        }
                    }
                }

                foreach (var windowInfo in processToWindowInfo.Values)
                {
                    this.windowInfos.Add(windowInfo);
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
        }, DispatcherPriority.Loaded);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        this.keyboardHook = new LowLevelKeyboardHook(PresentationSource.FromVisual(this)!);
        this.keyboardHook.LowLevelKeyUp += KeyboardHook_LowLevelKeyUp;
        this.keyboardHook.Start();

        // load the window placement details from the user settings.
        SnoopWindowUtils.LoadWindowPlacement(this, Settings.Default.AppChooserWindowPlacement);
    }

    private static void KeyboardHook_LowLevelKeyUp(object sender, KeyEventArgs e)
    {
        if (Settings.Default?.GlobalHotKey?.Matches(null, e) == true)
        {
            var thread = new Thread(AttachToForegroundWindow)
            {
                Name = "Snoop_AttachToForegroundWindow_Thread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }

    private static void AttachToForegroundWindow()
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();

        if (foregroundWindow != IntPtr.Zero)
        {
            var windowInfo = WindowInfo.GetWindowInfo(foregroundWindow);

            if (windowInfo.IsValidProcess)
            {
                WindowFinder.AttachSnoop(windowInfo);
            }
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // persist the window placement details to the user settings.
        SnoopWindowUtils.SaveWindowPlacement(this, wp => Settings.Default.AppChooserWindowPlacement = wp);

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        this.keyboardHook?.Stop();

        base.OnClosed(e);
    }

    private void HandleCanInspectOrMagnifyCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        if (this.WindowInfos.CurrentItem is not null)
        {
            e.CanExecute = true;
        }

        e.Handled = true;
    }

    private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e)
    {
        var window = (WindowInfo?)this.WindowInfos.CurrentItem;

        if (window is null)
        {
            return;
        }

        var result = window.OwningProcessInfo?.Snoop(window.HWnd);

        if (result?.Success == false)
        {
            ErrorDialog.ShowDialog(result.AttachException, "Can't Snoop the process", $"Failed to attach to '{window.Description}'.", true);
        }
    }

    private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e)
    {
        var window = (WindowInfo?)this.WindowInfos.CurrentItem;

        if (window is null)
        {
            return;
        }

        var result = window.OwningProcessInfo?.Magnify(window.HWnd);

        if (result?.Success == false)
        {
            ErrorDialog.ShowDialog(result.AttachException, "Can't Snoop the process", $"Failed to attach to '{window.Description}'.", true);
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
        var window = new SnoopBaseWindow
        {
            Content = new SettingsView(),
            Title = "Settings",
            Owner = this,
            MinWidth = 480,
            MinHeight = 320,
            Width = 480,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowStyle = WindowStyle.ToolWindow
        };

        if (window.ShowDialog() != true)
        {
            // Reload here to require users to explicitly save the settings from the dialog. Reload just discards any unsaved changes.
            Settings.Default.Reload();
        }
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

    /// <summary>
    /// Catch rmb from TextBlock of combobox drop down list to implement sorting.
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="e">Arguments</param>
    private void HandleDropDownColumn_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock textBlock)
        {
            return;
        }

        var gridColumn = (int)textBlock.GetValue(Grid.ColumnProperty);
        this.SortColumn = gridColumn;

        var propertyName = string.Empty;
        switch (gridColumn)
        {
            //by pid
            case 1:
                propertyName = nameof(WindowInfo.OwningProcessId);
                break;
            //by process name
            case 2:
                propertyName = nameof(WindowInfo.ProcessName);
                break;
            //by window name
            case 3:
                propertyName = nameof(WindowInfo.WindowTitle);
                break;
        }

        //read current sort order
        var sortDirection = this.WindowInfos.SortDescriptions.FirstOrDefault().Direction;

        //1 - read current target property for sorting.
        //2 - if current property not changed from last click - invert direction
        //3 - if property changed - direction does not change
        var currentSortProperty = this.WindowInfos.SortDescriptions.FirstOrDefault().PropertyName;
        if (string.Equals(currentSortProperty, propertyName))
        {
            //toggle sort direction
            sortDirection = sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        }

        this.WindowInfos.SortDescriptions.Clear();
        this.WindowInfos.SortDescriptions.Add(new SortDescription(propertyName, sortDirection));
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

    public bool Success { get; }

    public Exception? AttachException { get; }
}