// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Windows;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using JetBrains.Annotations;
using Snoop.Core;
using Snoop.Data.Tree;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Helpers;
using Snoop.Views;

public sealed partial class SnoopUI : INotifyPropertyChanged
{
    #region Public Static Routed Commands

    public static readonly RoutedCommand IntrospectCommand = new(nameof(IntrospectCommand), typeof(SnoopUI));
    public static readonly RoutedCommand RefreshCommand = new(nameof(RefreshCommand), typeof(SnoopUI));
    public static readonly RoutedCommand ExportTreeWithFilterCommand = new(nameof(ExportTreeWithFilterCommand), typeof(SnoopUI));
    public static readonly RoutedCommand HelpCommand = new(nameof(HelpCommand), typeof(SnoopUI));
    public static readonly RoutedCommand InspectCommand = new(nameof(InspectCommand), typeof(SnoopUI));
    public static readonly RoutedCommand SelectFocusCommand = new(nameof(SelectFocusCommand), typeof(SnoopUI));
    public static readonly RoutedCommand SelectFocusScopeCommand = new(nameof(SelectFocusScopeCommand), typeof(SnoopUI));
    public static readonly RoutedCommand ClearSearchFilterCommand = new(nameof(ClearSearchFilterCommand), typeof(SnoopUI));
    public static readonly RoutedCommand CopyPropertyChangesCommand = new(nameof(CopyPropertyChangesCommand), typeof(SnoopUI));

    #endregion

    static SnoopUI()
    {
        IntrospectCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
        RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
        HelpCommand.InputGestures.Add(new KeyGesture(Key.F1));
        ClearSearchFilterCommand.InputGestures.Add(new KeyGesture(Key.Escape));
        CopyPropertyChangesCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
    }

    public SnoopUI()
    {
        this.TreeService = TreeService.From(this.CurrentTreeType);

        this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

        this.InitializeComponent();

        PresentationTraceSourcesHelper.RefreshAndEnsureRequiredLevel();

        this.CommandBindings.Add(new(ApplicationCommands.Close, (_, _) => this.Close()));

        this.CommandBindings.Add(new CommandBinding(IntrospectCommand, this.HandleIntrospection));
        this.CommandBindings.Add(new CommandBinding(RefreshCommand, this.HandleRefresh));
        this.CommandBindings.Add(new CommandBinding(ExportTreeWithFilterCommand, this.HandleExport));
        this.CommandBindings.Add(new CommandBinding(HelpCommand, this.HandleHelp));

        this.CommandBindings.Add(new CommandBinding(InspectCommand, this.HandleInspect));

        this.CommandBindings.Add(new CommandBinding(SelectFocusCommand, this.HandleSelectFocus));
        this.CommandBindings.Add(new CommandBinding(SelectFocusScopeCommand, this.HandleSelectFocusScope));

        //NOTE: this is up here in the outer UI layer so ESC will clear any typed filter regardless of where the focus is
        // (i.e. focus on a selected item in the tree, not in the property list where the search box is hosted)
        this.CommandBindings.Add(new CommandBinding(ClearSearchFilterCommand, this.ClearSearchFilterHandler));

        this.CommandBindings.Add(new CommandBinding(CopyPropertyChangesCommand, this.CopyPropertyChangesHandler));

        InputManager.Current.PreProcessInput += this.HandlePreProcessInput;
        this.Tree.SelectedItemChanged += this.HandleTreeSelectedItemChanged;

        this.filterTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.3)
        };
        this.filterTimer.Tick += (_, _) =>
        {
            this.EnqueueAfterSettingFilter();
            this.filterTimer.Stop();
        };

        {
            this.snoopVersion.Header = $"Version: {this.GetType().Assembly.GetName().Version}";
        }
    }

    #region Public Properties

    #region Tree

    public TreeService TreeService
    {
        get => this.treeService;
        private set
        {
            if (Equals(value, this.treeService))
            {
                return;
            }

            this.treeService = value;
            this.OnPropertyChanged();
        }
    }

    /// <summary>
    /// This is the collection the TreeView binds to.
    /// </summary>
    public ObservableCollection<TreeItem> TreeItems { get; } = new();

    #endregion

    #region RootTreeItem

    public SystemResourcesTreeItem? SystemResourcesTreeItem { get; private set; }

    /// <summary>
    /// Root element of the tree.
    /// </summary>
    public TreeItem? RootTreeItem
    {
        get => this.rootTreeItem;

        private set
        {
            this.rootTreeItem = value;
            this.OnPropertyChanged();
        }
    }

    /// <summary>
    /// Root element of the tree.
    /// </summary>
    private TreeItem? rootTreeItem;

    #endregion

    #region CurrentSelection

    public override object? Target
    {
        get => this.CurrentSelection?.Target;
        set => this.CurrentSelection = this.FindItem(value);
    }

    /// <summary>
    /// Currently selected item in the tree view.
    /// </summary>
    public TreeItem? CurrentSelection
    {
        get => this.currentSelection;

        set
        {
            if (this.currentSelection == value)
            {
                return;
            }

            if (this.currentSelection is not null)
            {
                this.SaveEditedProperties(this.currentSelection);
                this.currentSelection.IsSelected = false;
            }

            this.currentSelection = value;

            if (this.CurrentSelection is not null)
            {
                this.CurrentSelection.IsSelected = true;
            }

            if (this.isShuttingDown)
            {
                return;
            }

            this.OnPropertyChanged();

            if (this.currentSelection is null)
            {
                return;
            }

            if (this.TreeItems.Count > 1
                || (this.TreeItems.Count == 1 && this.TreeItems[0] != this.RootTreeItem)
                || (this.TreeItems.Count == 2 && this.TreeItems[1] != this.RootTreeItem))
            {
                // Check whether the selected item is filtered out by the filter,
                // in which case reset the filter.
                var tmp = this.CurrentSelection;

                while (tmp is not null && !this.TreeItems.Contains(tmp))
                {
                    tmp = tmp.Parent;
                }

                if (tmp is null)
                {
                    // The selected item is not a descendant of any root.
                    RefreshCommand.Execute(null, this);
                }
            }
        }
    }

    private TreeItem? currentSelection;

    #endregion

    #region Filter

    /// <summary>
    /// This Filter property is bound to the editable combo box that the user can type in to filter the visual tree TreeView.
    /// Every time the user types a key, the setter gets called, enqueueing a delayed call to the ProcessFilter method.
    /// </summary>
    public string Filter
    {
        get { return this.filter; }

        set
        {
            this.filter = value;

            if (this.fromTextBox is false)
            {
                this.EnqueueAfterSettingFilter();
            }
            else
            {
                this.filterTimer.Stop();
                this.filterTimer.Start();
            }

            this.OnPropertyChanged();
        }
    }

    private void SetFilter(string value)
    {
        this.fromTextBox = false;
        this.Filter = value;
        this.fromTextBox = true;
    }

    private void EnqueueAfterSettingFilter()
    {
        this.filterCall.Enqueue(this.Dispatcher);

        this.OnPropertyChanged(nameof(this.Filter));
    }

    private string filter = string.Empty;

    #endregion

    #region Focus

    public IInputElement? CurrentFocus
    {
        get
        {
            var newFocus = Keyboard.FocusedElement;

            if (newFocus == this.currentFocus
                || (newFocus is null && this.IsActive))
            {
                return this.currentFocus;
            }

            if (SnoopPartsRegistry.IsSnoopingSnoop == false
                && newFocus is DependencyObject dpo
                && dpo.IsPartOfSnoopVisualTree())
            {
                return this.previousFocus;
            }

            this.currentFocus = newFocus;

            var result = this.returnPreviousFocus ? this.previousFocus : this.currentFocus;

            // Store reference to previously focused element only if focused element was changed.
            this.previousFocus = this.currentFocus;

            this.OnPropertyChanged(nameof(this.CurrentFocusScope));

            return result;
        }
    }

    public object? CurrentFocusScope
    {
        get
        {
            if (this.currentFocus is DependencyObject selectedItem)
            {
                return FocusManager.GetFocusScope(selectedItem);
            }

            return null;
        }
    }

    #endregion

    // ReSharper disable once InconsistentNaming
    public bool IsHandlingCTRL_SHIFT { get; set; } = true;

    public bool IgnoreHitTestVisibility { get; set; } = true;

    // ReSharper disable once InconsistentNaming
    public bool SkipTemplateParts { get; set; } = false;

    /// <summary>Identifies the <see cref="CurrentTreeType"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentTreeTypeProperty = DependencyProperty.Register(nameof(CurrentTreeType), typeof(TreeType), typeof(SnoopUI), new PropertyMetadata(TreeType.Visual, OnCurrentTreeTypeChanged));

    private static void OnCurrentTreeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SnoopUI)d;

        control.TreeService?.Dispose();
        control.TreeService = TreeService.From((TreeType)e.NewValue);
        control.Refresh();
    }

    public TreeType CurrentTreeType
    {
        get { return (TreeType)this.GetValue(CurrentTreeTypeProperty); }
        set { this.SetValue(CurrentTreeTypeProperty, value); }
    }

    #endregion

    #region Public Methods

    public void ApplyReduceDepthFilter(TreeItem? newRoot)
    {
        if (this.reducedDepthRoot == newRoot)
        {
            return;
        }

        // Check if we already have a scheduled reduce in progress
        if (this.IsReduceInProgress == false)
        {
            this.RunInDispatcherAsync(
                () =>
                {
                    this.TreeItems.Clear();

                    if (this.reducedDepthRoot is not null)
                    {
                        this.TreeItems.Add(this.reducedDepthRoot);
                    }

                    this.reducedDepthRoot = null;
                }, DispatcherPriority.Background);
        }

        this.reducedDepthRoot = newRoot;
    }

    public bool IsReduceInProgress => this.reducedDepthRoot is not null;

    /// <summary>
    /// Loop through the properties in the current PropertyGrid and save away any properties
    /// that have been changed by the user.
    /// </summary>
    /// <param name="owningObject">currently selected object that owns the properties in the grid (before changing selection to the new object)</param>
    private void SaveEditedProperties(TreeItem owningObject)
    {
        foreach (var property in this.PropertyGrid.PropertyGrid.Properties)
        {
            if (property.IsValueChangedByUser)
            {
                EditedPropertiesHelper.AddEditedProperty(this.Dispatcher, owningObject, property);
            }
        }
    }

    #endregion

    #region Protected Event Overrides

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        CacheManager.Instance.IncreaseUsageCount();

        // load whether all properties are shown by default
        this.PropertyGrid.ShowDefaults = Settings.Default.ShowDefaults;

        // load whether the previewer is shown by default
        this.PreviewArea.IsActive = Settings.Default.ShowPreviewer;

        // load the window placement details from the user settings.
        SnoopWindowUtils.LoadWindowPlacement(this, Settings.Default.SnoopUIWindowPlacement);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // persist the window placement details to the user settings.
        SnoopWindowUtils.SaveWindowPlacement(this, wp => Settings.Default.SnoopUIWindowPlacement = wp);

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        this.isShuttingDown = true;
        this.CurrentSelection = null;

        this.TreeService?.Dispose();

        this.eventsView?.Dispose();
        this.debugListenerControl?.Dispose();

        CacheManager.Instance.DecreaseUsageCount();

        InputManager.Current.PreProcessInput -= this.HandlePreProcessInput;

        this.filterTimer.Stop();

        // persist whether all properties are shown by default
        Settings.Default.ShowDefaults = this.PropertyGrid.ShowDefaults;

        // persist whether the previewer is shown by default
        Settings.Default.ShowPreviewer = this.PreviewArea?.IsActive == true;

        // actually do the persisting
        Settings.Default.Save();

        base.OnClosed(e);
    }

    #endregion

    #region Private Routed Event Handlers

    /// <summary>
    /// Just for fun, the ability to run Snoop on itself :)
    /// </summary>
    private void HandleIntrospection(object sender, ExecutedRoutedEventArgs e)
    {
        this.Load(this);
    }

    private void HandleRefresh(object sender, ExecutedRoutedEventArgs e)
    {
        this.Refresh();
    }

    private void Refresh()
    {
        var saveCursor = Mouse.OverrideCursor;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var previousSelection = this.CurrentSelection;
            var previousTarget = previousSelection?.Target;

            SystemResourcesCache.Instance.Reload();

            this.TreeItems.Clear();

            this.SystemResourcesTreeItem?.Dispose();
            this.SystemResourcesTreeItem = (SystemResourcesTreeItem)new SystemResourcesTreeItem(null, this.TreeService).Reload();

            this.RootTreeItem?.Dispose();
            this.RootTreeItem = this.TreeService.Construct(this.RootObject!, null);

            this.TreeService.DiagnosticContext.AnalyzeTree();

            if (previousTarget is not null)
            {
                var treeItem = this.FindItem(previousTarget);
                if (treeItem is not null)
                {
                    this.CurrentSelection = treeItem;
                    this.PropertyGrid.PropertyGrid.RefreshPropertyGrid();

                    if (previousSelection?.IsExpanded == true)
                    {
                        this.CurrentSelection.ExpandTo();
                    }
                }
                else
                {
                    this.CurrentSelection = null;
                }
            }

            this.SetFilter(this.filter);
        }
        finally
        {
            Mouse.OverrideCursor = saveCursor;
        }
    }

    private void HandleExport(object sender, ExecutedRoutedEventArgs e)
    {
        if (this.CurrentSelection is null)
        {
            return;
        }

        Cursor saveCursor = Mouse.OverrideCursor;
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            var options = (ExportOptions)e.Parameter;
            var propertyFilter = options.UseFilter ? this.PropertyGrid.PropertyFilter : null;

            var treeItem = options.TreeItem ?? this.CurrentSelection;

            if (treeItem is null)
            {
                MessageBox.Show("No tree item found for export.", "Tree not exported", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SnoopTreeExport");

            Directory.CreateDirectory(targetFolder);

            using var proc = Process.GetCurrentProcess();
            var exportDateTimeText = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            var file = Path.Combine(targetFolder, $"{proc.ProcessName} - [{proc.Id}] ({exportDateTimeText}).xml");

            // Append "(n)" suffix to avoid overwriting existing files.
            for (var i = 1; i < 10000; i++)
            {
                if (File.Exists(file) == false)
                {
                    break;
                }

                file = Path.Combine(targetFolder, $"{proc.ProcessName} - [{proc.Id}] ({exportDateTimeText}) ({i}).xml");
            }

            using var streamWriter = new StreamWriter(file, false, Encoding.UTF8);

            TreeExporter.Export(treeItem, streamWriter, propertyFilter, options.Recurse);

            MessageBox.Show($"The tree has been exported to \"{file}\".", "Tree exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            Mouse.OverrideCursor = saveCursor;
        }
    }

    private void HandleHelp(object sender, ExecutedRoutedEventArgs e)
    {
        //Help help = new Help();
        //help.Show();
    }

    private void HandleInspect(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is Visual visual)
        {
            var node = this.FindItem(visual);
            if (node is not null)
            {
                this.CurrentSelection = node;
            }
        }
        else if (e.Parameter is not null)
        {
            this.PropertyGrid.SetTarget(e.Parameter);
        }
    }

    private void HandleSelectFocus(object sender, ExecutedRoutedEventArgs e)
    {
        // We know we've stolen focus here. Let's use previously focused element.
        this.returnPreviousFocus = true;
        this.SelectItem(this.CurrentFocus as DependencyObject);
        this.returnPreviousFocus = false;
        this.OnPropertyChanged(nameof(this.CurrentFocus));
    }

    private void HandleSelectFocusScope(object sender, ExecutedRoutedEventArgs e)
    {
        this.SelectItem(e.Parameter as DependencyObject);
    }

    private void ClearSearchFilterHandler(object sender, ExecutedRoutedEventArgs e)
    {
        this.PropertyGrid.FilterString = string.Empty;
    }

    private void CopyPropertyChangesHandler(object sender, ExecutedRoutedEventArgs e)
    {
        if (this.CurrentSelection is not null)
        {
            this.SaveEditedProperties(this.CurrentSelection);
        }

        EditedPropertiesHelper.DumpObjectsWithEditedProperties();
    }

    private void SelectItem(DependencyObject? item)
    {
        if (item is not null)
        {
            var node = this.FindItem(item);
            if (node is not null)
            {
                this.CurrentSelection = node;
            }
        }
    }
    #endregion

    #region Private Event Handlers

    private void HandlePreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        this.OnPropertyChanged(nameof(this.CurrentFocus));

        if (this.IsHandlingCTRL_SHIFT == false)
        {
            return;
        }

        var currentModifiers = InputManager.Current.PrimaryKeyboardDevice.Modifiers;

        var isControlPressed = currentModifiers.HasFlag(ModifierKeys.Control);
        var isShiftPressed = currentModifiers.HasFlag(ModifierKeys.Shift);

        if (isControlPressed == false
            || isShiftPressed == false)
        {
            return;
        }

        var itemToFind = Mouse.PrimaryDevice.GetDirectlyOver(this.IgnoreHitTestVisibility);

        switch (itemToFind)
        {
            case null:
            case var dependencyObject when dependencyObject.IsPartOfSnoopVisualTree():
            case Visual visual when visual.IsDescendantOf(this):
                return;
        }

        // If template parts should be skipped search up the tree of templated parents.
        if (this.SkipTemplateParts
            && itemToFind is FrameworkElement frameworkElement)
        {
            itemToFind = GetItemToFindAndSkipTemplateParts(frameworkElement);
        }

        var node = this.FindItem(itemToFind);
        if (node is not null
            && ReferenceEquals(this.CurrentSelection, node) == false)
        {
            this.CurrentSelection = node;
        }
    }

    private static UIElement? GetItemToFindAndSkipTemplateParts(FrameworkElement? uiElement)
    {
        UIElement? itemToFind = uiElement;

        while (uiElement?.TemplatedParent is not null)
        {
            uiElement = uiElement.TemplatedParent as FrameworkElement;
            itemToFind = uiElement;
        }

        if (itemToFind is not null)
        {
            var parent = VisualTreeHelper.GetParent(itemToFind) as FrameworkElement;

            // If the current item is of a certain type and is part of a template try to look further up the tree
            if (parent is FrameworkElement { TemplatedParent: { } } and (ContentPresenter or AccessText))
            {
                return GetItemToFindAndSkipTemplateParts(parent);
            }
        }

        return itemToFind;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Find the TreeItem for the specified target.
    /// If the item is not found and is not part of the Snoop UI,
    /// the tree will be adjusted to include the root visual the item is in.
    /// </summary>
    private TreeItem? FindItem(object? target)
    {
        if (this.RootTreeItem is null)
        {
            return null;
        }

        {
            var node = this.RootTreeItem.FindNode(target);

            if (node is not null)
            {
                return node;
            }
        }

        // Not every visual element is in the logical or the automation tree, so try the visual tree
        if (this.TreeService.TreeType is TreeType.Logical or TreeType.Automation
            && target is DependencyObject dependencyObject and (Visual or Visual3D))
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            while (parent is not null)
            {
                var node = this.RootTreeItem.FindNode(parent);

                if (node is not null)
                {
                    return node;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        var newRootSet = false;
        var rootVisual = this.RootTreeItem.MainVisual;

        if (target is Visual visual
            && rootVisual is not null)
        {
            // If target is a part of the SnoopUI, let's get out of here.
            if (visual.IsDescendantOf(this))
            {
                return null;
            }

            // If not in the root tree, make the root be the tree the visual is in.
            if (visual.IsDescendantOf(rootVisual) == false)
            {
                var presentationSource = PresentationSource.FromVisual(visual);
                if (presentationSource is null)
                {
                    return null; // Something went wrong. At least we will not crash with null ref here.
                }

                this.SystemResourcesTreeItem = (SystemResourcesTreeItem)new SystemResourcesTreeItem(null, this.TreeService).Reload();
                this.RootTreeItem = this.TreeService.Construct(presentationSource.RootVisual, null);
                newRootSet = true;
            }
        }

        // Constructing a new root already reloads it
        if (newRootSet == false)
        {
            this.SystemResourcesTreeItem?.Reload();
            this.RootTreeItem.Reload();
        }

        this.TreeService.DiagnosticContext.AnalyzeTree();

        {
            var node = this.RootTreeItem.FindNode(target);

            // Tree items are cleared when filtering
            this.SetFilter(this.filter);

            return node;
        }
    }

    private void HandleTreeSelectedItemChanged(object sender, EventArgs e)
    {
        if (this.Tree.SelectedItem is TreeItem item)
        {
            this.CurrentSelection = item;
        }
    }

    private void ProcessFilter()
    {
        if (SnoopModes.MultipleDispatcherMode
            && this.Dispatcher.CheckAccess() == false)
        {
            this.RunInDispatcherAsync(this.ProcessFilter);
            return;
        }

        this.TreeItems.Clear();

        // cplotts todo: we've got to come up with a better way to do this.
        if (this.filter == "Clear any filter applied to the tree view")
        {
            this.SetFilter(string.Empty);
        }
        else if (this.filter == "Show only elements with binding errors")
        {
            this.FilterBindings(this.RootTreeItem!);
        }
        else if (this.filter.Length == 0)
        {
            this.TreeItems.Add(this.SystemResourcesTreeItem!);
            this.TreeItems.Add(this.RootTreeItem!);
        }
        else
        {
            this.FilterTree(this.RootTreeItem!, this.filter.ToLower());
        }
    }

    private void FilterTree(TreeItem node, string currentFilter)
    {
        foreach (var child in node.Children)
        {
            if (child.Filter(currentFilter))
            {
                this.TreeItems.Add(child);
            }
            else
            {
                this.FilterTree(child, currentFilter);
            }
        }
    }

    private void FilterBindings(TreeItem node)
    {
        foreach (var child in node.Children)
        {
            if (child.HasBindingError)
            {
                this.TreeItems.Add(child);
            }
            else
            {
                this.FilterBindings(child);
            }
        }
    }

    protected override void Load(object newRoot)
    {
        this.Refresh();

        this.CurrentSelection = this.RootTreeItem;
    }

    #endregion

    #region Private Fields

    private bool fromTextBox = true;
    private readonly DispatcherTimer filterTimer;

    private readonly DelayedCall filterCall;

    private TreeItem? reducedDepthRoot;

    private IInputElement? currentFocus;
    private IInputElement? previousFocus;

    /// <summary>
    /// Indicates whether CurrentFocus should return previously focused element.
    /// This fixes problem where Snoop steals the focus from snooped app.
    /// </summary>
    private bool returnPreviousFocus;

    private TreeService treeService = null!;

    private bool isShuttingDown;

    #endregion

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    private void HandleMakeSettingsApplicationsSpecific_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Default.SettingsFile = SettingsHelper.GetApplicationSpecificSettingsFile();
        Settings.Default.Save();
    }

    private void HandleDeleteApplicationSpecificSettings_OnClick(object sender, RoutedEventArgs e)
    {
        // Prevent accidental delete of default settings file
        if (Settings.Default.IsDefaultSettingsFile == false)
        {
            File.Delete(Settings.Default.SettingsFile);
        }

        Settings.Default.SettingsFile = SettingsHelper.GetDefaultApplicationSettingsFile();

        Settings.Default.Reload();
    }

    private void HandleOpenSettingsFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var directory = Path.GetDirectoryName(Settings.Default.SettingsFile);

        if (directory is not null
            && directory.Length > 0)
        {
            var processStartInfo = new ProcessStartInfo(directory)
            {
                UseShellExecute = true
            };
            using (Process.Start(processStartInfo))
            {
            }
        }
    }

    private void HandleResetSettings_OnClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Are you sure that you want to reset all settings?", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.No)
        {
            return;
        }

        Settings.Default.Reset();

        // load whether all properties are shown by default
        this.PropertyGrid.ShowDefaults = Settings.Default.ShowDefaults;

        // load whether the previewer is shown by default
        this.PreviewArea.IsActive = Settings.Default.ShowPreviewer;

        this.PropertyGrid.checkBoxClearAfterDelve.IsChecked = Settings.Default.ClearAfterDelve;

        this.eventsView.UpdateTrackers();
        this.eventsView.MaxEventsDisplayed = Settings.Default.MaximumTrackedEvents;

        this.debugListenerControl.FiltersViewModel.InitializeFilters(Settings.Default.SnoopDebugFilters);
    }

    private void HandleLaunchDebugger_OnClick(object sender, RoutedEventArgs e)
    {
        Debugger.Launch();
    }

    private void HandleSnoopSnoop_OnClick(object sender, RoutedEventArgs e)
    {
        SnoopPartsRegistry.IsSnoopingSnoop = true;

        new SnoopUI().Inspect(this);
    }

    private void HandleHighlightOptions_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new SnoopBaseWindow
        {
            Content = new HighlightSettingsView(),
            Title = "Highlight options",
            Owner = this,
            MinWidth = 480,
            MinHeight = 360,
            Width = 480,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowStyle = WindowStyle.ToolWindow
        };
        window.ShowDialog();
    }

    private void HandleSnoopVersion_OnClick(object sender, RoutedEventArgs e)
    {
        ClipboardHelper.SetText((string)this.snoopVersion.Header);
    }
}

public class PropertyValueInfo
{
    public PropertyValueInfo(string name, object? value)
    {
        this.PropertyName = name;
        this.PropertyValue = value;
    }

    public string PropertyName { get; }

    public object? PropertyValue { get; }
}

public static class EditedPropertiesHelper
{
    private static readonly object @lock = new();

    private static readonly Dictionary<Dispatcher, Dictionary<TreeItem, List<PropertyValueInfo>>> itemsWithEditedProperties =
        new();

    public static void AddEditedProperty(Dispatcher dispatcher, TreeItem propertyOwner, PropertyInformation propInfo)
    {
        lock (@lock)
        {
            // first get the dictionary we're using for the given dispatcher
            if (!itemsWithEditedProperties.TryGetValue(dispatcher, out var dispatcherList))
            {
                dispatcherList = new Dictionary<TreeItem, List<PropertyValueInfo>>();
                itemsWithEditedProperties.Add(dispatcher, dispatcherList);
            }

            // now get the property info list for the owning object
            if (!dispatcherList.TryGetValue(propertyOwner, out var propInfoList))
            {
                propInfoList = new List<PropertyValueInfo>();
                dispatcherList.Add(propertyOwner, propInfoList);
            }

            // if we already have a property of that name on this object, remove it
            var existingPropInfo = propInfoList.FirstOrDefault(l => l.PropertyName == propInfo.DisplayName);
            if (existingPropInfo is not null)
            {
                propInfoList.Remove(existingPropInfo);
            }

            // finally add the edited property info
            propInfoList.Add(new PropertyValueInfo(propInfo.DisplayName, propInfo.Value));
        }
    }

    public static void DumpObjectsWithEditedProperties()
    {
        lock (@lock)
        {
            if (itemsWithEditedProperties.Count == 0)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat(
                "Snoop dump as of {0:yyyy-MM-dd HH:mm:ss}{1}--- OBJECTS WITH EDITED PROPERTIES ---{1}",
                DateTime.Now,
                Environment.NewLine);

            var dispatcherCount = 1;

            foreach (var dispatcherItemPair in itemsWithEditedProperties)
            {
                if (itemsWithEditedProperties.Count > 1)
                {
                    sb.AppendFormat("-- Dispatcher #{0} -- {1}", dispatcherCount++, Environment.NewLine);
                }

                foreach (var objectPropertiesItemPair in dispatcherItemPair.Value)
                {
                    sb.AppendFormat("Object: {0}{1}", objectPropertiesItemPair.Key, Environment.NewLine);
                    foreach (var propInfo in objectPropertiesItemPair.Value)
                    {
                        sb.AppendFormat(
                            "\tProperty: {0}, New Value: {1}{2}",
                            propInfo.PropertyName,
                            propInfo.PropertyValue,
                            Environment.NewLine);
                    }
                }

                if (itemsWithEditedProperties.Count > 1)
                {
                    sb.AppendLine();
                }
            }

            LogHelper.WriteLine(sb.ToString());
            ClipboardHelper.SetText(sb.ToString());
        }
    }
}