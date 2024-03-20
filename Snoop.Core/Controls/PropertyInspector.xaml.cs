// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma warning disable CA1819

namespace Snoop.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using Snoop.Converters;
using Snoop.Core;
using Snoop.Data;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Helpers;
using Snoop.Windows;

public partial class PropertyInspector : INotifyPropertyChanged
{
    public static readonly RoutedCommand PopTargetCommand = new(nameof(PopTargetCommand), typeof(PropertyInspector));

    public static readonly RoutedCommand DelveCommand = new(nameof(DelveCommand), typeof(PropertyInspector));
    public static readonly RoutedCommand DelveBindingCommand = new(nameof(DelveBindingCommand), typeof(PropertyInspector));
    public static readonly RoutedCommand DelveBindingExpressionCommand = new(nameof(DelveBindingExpressionCommand), typeof(PropertyInspector));
    public static readonly RoutedCommand CopyResourceNameCommand = new(nameof(CopyResourceNameCommand), typeof(PropertyInspector));
    public static readonly RoutedCommand CopyXamlCommand = new(nameof(CopyXamlCommand), typeof(PropertyInspector));
    public static readonly RoutedCommand CopyFQNameCommand = new(nameof(CopyFQNameCommand), typeof(PropertyInspector));

    public static readonly RoutedCommand NavigateToAssemblyInExplorerCommand = new(nameof(NavigateToAssemblyInExplorerCommand), typeof(PropertyInspector));
    public static readonly RoutedCommand OpenTypeInILSpyCommand = new(nameof(OpenTypeInILSpyCommand), typeof(PropertyInspector));

    public static readonly RoutedCommand UpdateBindingErrorCommand = new(nameof(UpdateBindingErrorCommand), typeof(PropertyInspector));

    private object? target;

    public PropertyInspector()
    {
        this.InitializeComponent();

        this.delveTypeContextMenu.DataContext = this;

        this.inspector = this.PropertyGrid;
        this.inspector.Filter = this.propertyFilter;

        this.CommandBindings.Add(new CommandBinding(PopTargetCommand, this.HandlePopTarget, this.CanPopTarget));

        this.CommandBindings.Add(new CommandBinding(DelveCommand, this.HandleDelve, CanDelve));
        this.CommandBindings.Add(new CommandBinding(DelveBindingCommand, this.HandleDelveBinding, CanDelveBinding));
        this.CommandBindings.Add(new CommandBinding(DelveBindingExpressionCommand, this.HandleDelveBindingExpression, CanDelveBindingExpression));
        this.CommandBindings.Add(new CommandBinding(CopyResourceNameCommand, this.HandleCopyResourceName, this.CanCopyResourceName));
        this.CommandBindings.Add(new CommandBinding(CopyXamlCommand, this.HandleCopyXaml, this.CanCopyXaml));
        this.CommandBindings.Add(new CommandBinding(CopyFQNameCommand, this.HandleCopyFQName, this.CanCopyFQName));

        this.CommandBindings.Add(new CommandBinding(NavigateToAssemblyInExplorerCommand, this.HandleNavigateToAssemblyInExplorer, this.CanNavigateToAssemblyInExplorer));
        this.CommandBindings.Add(new CommandBinding(OpenTypeInILSpyCommand, this.HandleOpenTypeInILSpy, this.CanOpenTypeInILSpy));
        this.CommandBindings.Add(new CommandBinding(UpdateBindingErrorCommand, this.HandleUpdateBindingError, this.CanUpdateBindingError));

        // watch for mouse "back" button
        this.MouseDown += this.MouseDownHandler;
        this.KeyDown += this.PropertyInspector_KeyDown;

        this.checkBoxClearAfterDelve.Checked += (_, _) => Settings.Default.ClearAfterDelve = this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value;
        this.checkBoxClearAfterDelve.Unchecked += (_, _) => Settings.Default.ClearAfterDelve = this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value;

        this.checkBoxClearAfterDelve.IsChecked = Settings.Default.ClearAfterDelve;
    }

    public bool NameValueOnly
    {
        get
        {
            return this.PropertyGrid.NameValueOnly;
        }

        set
        {
            this.PropertyGrid.NameValueOnly = value;
        }
    }

    private void HandleCopyResourceName(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var stringValue = StringValueConverter.ConvertToString(((PropertyInformation?)e.Parameter)?.ResourceKey);
            ClipboardHelper.SetText(stringValue ?? string.Empty);
        }
        catch (Exception exception)
        {
            ErrorDialog.ShowExceptionMessageBox(exception);
        }
    }

    private void CanCopyResourceName(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation)
        {
            e.CanExecute = ResourceKeyHelper.IsValidResourceKey(propertyInformation.ResourceKey);
        }

        e.Handled = true;
    }

    private void HandleCopyXaml(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var value = ((PropertyInformation?)e.Parameter)?.Value;
            var xaml = XamlWriterHelper.GetXamlAsString(value);
            ClipboardHelper.SetText(xaml);
        }
        catch (Exception exception)
        {
            ErrorDialog.ShowExceptionMessageBox(exception);
        }
    }

    private void CanCopyXaml(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation
            && propertyInformation.Value is not null)
        {
            e.CanExecute = true;
        }

        e.Handled = true;
    }

    private void HandleCopyFQName(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var type = (e.Parameter as Type) ?? (BindableType)e.Parameter;
            ClipboardHelper.SetText(type.FullName ?? string.Empty);
        }
        catch (Exception exception)
        {
            ErrorDialog.ShowExceptionMessageBox(exception);
        }
    }

    private void CanCopyFQName(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = e.Parameter is System.Type or BindableType;
        e.Handled = true;
    }

    public object? RootTarget
    {
        get { return this.GetValue(RootTargetProperty); }
        set { this.SetValue(RootTargetProperty, value); }
    }

    public static readonly DependencyProperty RootTargetProperty =
        DependencyProperty.Register(
            nameof(RootTarget),
            typeof(object),
            typeof(PropertyInspector),
            new PropertyMetadata(OnRootTargetChanged));

    private static void OnRootTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var inspector = (PropertyInspector)d;

        inspector.inspectStack.Clear();
        inspector.Target = e.NewValue;

        inspector.delvePathList.Clear();
        inspector.OnPropertyChanged(nameof(DelvePath));
        inspector.OnPropertyChanged(nameof(DelveType));

        inspector.targetToFilter.Clear();
    }

    public object? Target
    {
        get => this.target;
        set
        {
            if (Equals(value, this.target))
            {
                return;
            }

            var oldValue = this.target;
            this.target = value;

            this.OnPropertyChanged(nameof(this.Target));
            this.OnPropertyChanged(nameof(this.Type));

            HandleTargetChanged(this, oldValue, value);
        }
    }

    private static void HandleTargetChanged(PropertyInspector inspector, object? oldValue, object? newValue)
    {
        if (newValue is not null)
        {
            inspector.inspectStack.Add(newValue);
        }

        if (ReferenceEquals(inspector.lastRootTarget, inspector.RootTarget)
            && oldValue is not null
            && newValue is not null
            && inspector.checkBoxClearAfterDelve.IsChecked.GetValueOrDefault(false))
        {
            inspector.targetToFilter[oldValue.GetType()] = inspector.PropertiesFilter.Text;
            inspector.targetToFilter.TryGetValue(newValue.GetType(), out var text);
            inspector.PropertiesFilter.Text = text ?? string.Empty;
        }

        inspector.lastRootTarget = inspector.RootTarget;
    }

    private string GetCurrentDelvePath(Type rootTargetType)
    {
        var delvePath = new StringBuilder(rootTargetType.Name);

        foreach (var propInfo in this.delvePathList)
        {
            if (propInfo.IsCollectionEntry)
            {
                delvePath.Append(string.Format("[{0}]", propInfo.CollectionEntryIndexOrKey));
            }
            else
            {
                delvePath.Append(string.Format(".{0}", propInfo.DisplayName));
            }
        }

        return delvePath.ToString();
    }

    private BindableType? GetCurrentDelveType(Type rootTargetType)
    {
        if (this.delvePathList.Count > 0)
        {
            var lastDelveEntry = this.delvePathList.Last();

            if (lastDelveEntry.Value is ISkipDelve skipDelve
                && skipDelve.NextValue is not null
                && skipDelve.NextValueType is not null)
            {
                return skipDelve.NextValueType; //we want to make this "future friendly", so we take into account that the string value of the property type may change.
            }
            else if (lastDelveEntry.Value is not null)
            {
                return lastDelveEntry.Value.GetType();
            }
            else
            {
                return lastDelveEntry.PropertyType;
            }
        }
        else if (this.delvePathList.Count == 0)
        {
            return rootTargetType;
        }

        return null;
    }

    /// <summary>
    /// Delve Path
    /// </summary>
    public string DelvePath
    {
        get
        {
            if (this.RootTarget is null)
            {
                return "Target object is NULL";
            }

            var rootTargetType = this.RootTarget.GetType();
            var delvePath = this.GetCurrentDelvePath(rootTargetType);

            return delvePath;
        }
    }

    public BindableType? DelveType
    {
        get
        {
            if (this.RootTarget is null)
            {
                return null;
            }

            var rootTargetType = this.RootTarget.GetType();
            return this.GetCurrentDelveType(rootTargetType);
        }
    }

    public BindableType? Type
    {
        get
        {
            if (this.Target is not null)
            {
                return this.Target.GetType();
            }

            return null;
        }
    }

    public void PushTarget(object? newTarget)
    {
        this.Target = newTarget;
    }

    public void SetTarget(object newTarget)
    {
        this.inspectStack.Clear();
        this.Target = newTarget;
    }

    private void HandlePopTarget(object sender, ExecutedRoutedEventArgs e)
    {
        this.PopTarget();
    }

    private void PopTarget()
    {
        if (this.inspectStack.Count > 1)
        {
            this.Target = this.inspectStack[this.inspectStack.Count - 2];
            this.inspectStack.RemoveAt(this.inspectStack.Count - 2);
            this.inspectStack.RemoveAt(this.inspectStack.Count - 2);

            if (this.delvePathList.Count > 0)
            {
                this.delvePathList.RemoveAt(this.delvePathList.Count - 1);
                this.OnPropertyChanged(nameof(this.DelvePath));
                this.OnPropertyChanged(nameof(this.DelveType));
            }
        }
    }

    private void CanPopTarget(object sender, CanExecuteRoutedEventArgs e)
    {
        if (this.inspectStack.Count > 1)
        {
            e.Handled = true;
            e.CanExecute = true;
        }
    }

    private object? GetRealTarget(object? newTarget)
    {
        if (newTarget is ISkipDelve skipDelve)
        {
            return skipDelve.NextValue;
        }

        return newTarget;
    }

    private void HandleDelve(object sender, ExecutedRoutedEventArgs e)
    {
        var realTarget = this.GetRealTarget(((PropertyInformation)e.Parameter).Value);

        if (realTarget != this.Target)
        {
            // top 'if' statement is the delve path.
            // we do this because without doing this, the delve path gets out of sync with the actual delves.
            // the reason for this is because PushTarget sets the new target,
            // and if it's equal to the current (original) target, we won't raise the property-changed event,
            // and therefore, we don't add to our delveStack (the real one).

            this.delvePathList.Add((PropertyInformation)e.Parameter);
            this.OnPropertyChanged(nameof(this.DelvePath));
            this.OnPropertyChanged(nameof(this.DelveType));
        }

        if (this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value)
        {
            this.PropertiesFilter.Focus();
        }

        this.PushTarget(realTarget);
    }

    private void HandleDelveBinding(object sender, ExecutedRoutedEventArgs e)
    {
        this.PushTarget(((PropertyInformation)e.Parameter).Binding);
    }

    private void HandleDelveBindingExpression(object sender, ExecutedRoutedEventArgs e)
    {
        this.PushTarget(((PropertyInformation)e.Parameter).BindingExpression);
    }

    private static void CanDelve(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation
            && propertyInformation.Value is not null)
        {
            e.CanExecute = true;
        }

        e.Handled = true;
    }

    private static void CanDelveBinding(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation
            && propertyInformation.Binding is not null)
        {
            e.CanExecute = true;
        }

        e.Handled = true;
    }

    private static void CanDelveBindingExpression(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation
            && propertyInformation.BindingExpression is not null)
        {
            e.CanExecute = true;
        }

        e.Handled = true;
    }

    private void HandleNavigateToAssemblyInExplorer(object sender, ExecutedRoutedEventArgs e)
    {
        var assembly = (e.Parameter as Type)?.Assembly ?? ((BindableType)e.Parameter).Assembly;
        var path = assembly.Location;

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "explorer",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
            Arguments = $"/e,/select,\"{path}\""
        };

        try
        {
            using (Process.Start(processStartInfo))
            {
            }
        }
        catch
        {
            // ignored
        }
    }

    private void CanNavigateToAssemblyInExplorer(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = e.Parameter is System.Type or BindableType;
    }

    private void HandleOpenTypeInILSpy(object sender, ExecutedRoutedEventArgs e)
    {
        var type = (e.Parameter as Type) ?? (BindableType)e.Parameter;
        var assembly = type.Assembly;
        var assemblyLocation = assembly.Location;

        if (string.IsNullOrEmpty(assemblyLocation)
            || PathHelper.TryFindPathOnPath(TransientSettingsData.Current?.ILSpyPath, "ilspy.exe", out var ilspyPath) == false)
        {
            return;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = ilspyPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
            // https://github.com/icsharpcode/ILSpy/blob/master/doc/Command%20Line.txt
            Arguments = $"\"{assemblyLocation}\" /navigateTo:T:{type.FullName}"
        };

        try
        {
            using (Process.Start(processStartInfo))
            {
            }
        }
        catch
        {
            // ignored
        }
    }

    private void CanOpenTypeInILSpy(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = e.Parameter is System.Type or BindableType
                       && PathHelper.TryFindPathOnPath(TransientSettingsData.Current?.ILSpyPath, "ilspy.exe", out _);
    }

    private void CanUpdateBindingError(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation)
        {
            e.CanExecute = propertyInformation.IsInvalidBinding
                           && string.IsNullOrEmpty(propertyInformation.BindingError);
        }
    }

    private void HandleUpdateBindingError(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation)
        {
            propertyInformation.UpdateBindingError();
        }
    }

    public PropertyFilter PropertyFilter
    {
        get { return this.propertyFilter; }
    }

    private readonly PropertyFilter propertyFilter = new(string.Empty, true);

    public string FilterString
    {
        get { return this.propertyFilter.FilterString; }

        set
        {
            this.propertyFilter.FilterString = value;

            this.inspector.Filter = this.propertyFilter;

            this.OnPropertyChanged(nameof(this.FilterString));
        }
    }

    public bool UseRegex
    {
        get { return this.propertyFilter.UseRegex; }

        set
        {
            this.propertyFilter.UseRegex = value;

            this.inspector.Filter = this.propertyFilter;

            this.OnPropertyChanged(nameof(this.UseRegex));
        }
    }

    public bool ShowDefaults
    {
        get { return this.propertyFilter.ShowDefaults; }

        set
        {
            this.propertyFilter.ShowDefaults = value;

            this.inspector.Filter = this.propertyFilter;

            this.OnPropertyChanged(nameof(this.ShowDefaults));
        }
    }

    public bool ShowUncommonProperties
    {
        get { return this.propertyFilter.ShowUncommonProperties; }

        set
        {
            this.propertyFilter.ShowUncommonProperties = value;

            this.inspector.Filter = this.propertyFilter;

            this.OnPropertyChanged(nameof(this.ShowUncommonProperties));
        }
    }

    /// <summary>
    /// Looking for "browse back" mouse button.
    /// Pop properties context when clicked.
    /// </summary>
    private void MouseDownHandler(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.XButton1)
        {
            this.PopTarget();
        }
    }

    private void PropertyInspector_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Left)
        {
            this.PopTarget();
        }
    }

    /// <summary>
    /// Hold the SelectedFilterSet in the PropertyFilter class, but track it here, so we know
    /// when to "refresh" the filtering with filterCall.Enqueue
    /// </summary>
    public PropertyFilterSet? SelectedFilterSet
    {
        get { return this.propertyFilter.SelectedFilterSet ?? this.AllFilterSets[0]; }

        set
        {
            this.propertyFilter.SelectedFilterSet = value;
            this.OnPropertyChanged(nameof(this.SelectedFilterSet));

            if (value is null)
            {
                return;
            }

            if (value.IsEditCommand)
            {
                var dlg = new EditUserFilters { UserFilters = this.CopyFilterSets(this.defaultFilterSets.ToList().Concat(this.UserFilterSets)) };

                var res = dlg.ShowDialogEx(this);

                if (res.GetValueOrDefault())
                {
                    // take the adjusted values from the dialog
                    this.UserFilterSets.UpdateWith(CleanFiltersForUserFilters(dlg.ItemsSource));

                    Settings.Default.Save();

#pragma warning disable INPC015
                    this.SelectedFilterSet = null;
#pragma warning restore INPC015
                    this.allFilterSets = null;

                    // trigger the UI to re-bind to the collection, so user sees changes they just made
                    this.OnPropertyChanged(nameof(this.AllFilterSets));
                }
            }
            else
            {
                this.inspector.Filter = this.propertyFilter;
            }

            this.OnPropertyChanged(nameof(this.SelectedFilterSet));
        }
    }

    /// <summary>
    /// Get or Set the collection of User filter sets.  These are the filters that are configurable by
    /// the user, and serialized to/from app Settings.
    /// </summary>
    public ObservableCollection<PropertyFilterSet> UserFilterSets { get; } = Settings.Default.UserDefinedPropertyFilterSets;

    /// <summary>
    /// Get the collection of "all" filter sets.  This is the UserFilterSets wrapped with
    /// (Default) at the start and "Edit Filters..." at the end of the collection.
    /// This is the collection bound to in the UI
    /// </summary>
    public PropertyFilterSet[] AllFilterSets
    {
        get
        {
            if (this.allFilterSets is not null)
            {
                return this.allFilterSets;
            }

            var ret = new List<PropertyFilterSet>(this.defaultFilterSets);
            ret.AddRange(this.UserFilterSets);

            // now add the "(Default)" and "Edit Filters..." filters for the ComboBox
            ret.Insert(
                0,
                new PropertyFilterSet
                {
                    DisplayName = "(Default)",
                    IsDefault = true,
                    IsEditCommand = false,
                });
            ret.Add(
                new PropertyFilterSet
                {
                    DisplayName = "Edit Filters...",
                    IsDefault = false,
                    IsEditCommand = true,
                });

            this.allFilterSets = ret.ToArray();

            return this.allFilterSets;
        }
    }

    /// <summary>
    /// Make a deep copy of the filter collection.
    /// This is used when heading into the Edit dialog, so the user is editing a copy of the
    /// filters, in case they cancel the dialog - we dont want to alter their live collection.
    /// </summary>
    public PropertyFilterSet[] CopyFilterSets(IEnumerable<PropertyFilterSet> source)
    {
        return source.Select(x => x.Clone()).ToArray();
    }

    /// <summary>
    /// Cleanse the property names in each filter in the collection.
    /// This includes removing spaces from each one, and making them all lower case
    /// </summary>
    private static List<PropertyFilterSet> CleanFiltersForUserFilters(ICollection<PropertyFilterSet>? collection)
    {
        if (collection is null)
        {
            return new List<PropertyFilterSet>();
        }

        foreach (var filterItem in collection)
        {
            filterItem.Properties = filterItem.Properties?.Select(s => s.ToLower().Trim()).ToArray();
        }

        return collection.Where(x => x.IsReadOnly == false).ToList();
    }

    private readonly List<object> inspectStack = new();
    private readonly List<PropertyInformation> delvePathList = new();

    private readonly Inspector inspector;
    private object? lastRootTarget;
    private readonly Dictionary<object, string> targetToFilter = new();

    private PropertyFilterSet[]? allFilterSets;

    private readonly PropertyFilterSet[] defaultFilterSets =
    {
        new()
        {
            DisplayName = "Layout",
            IsReadOnly = true,
            Properties = new[]
            {
                "width", "height", "minwidth", "minheight", "maxwidth", "maxheight", "actualwidth", "actualheight",
                "desiredsize", "rendersize", "availablesize",
                "margin", "padding",
                "left", "top",
                "borderthickness",
                "horizontalalignment", "verticalalignment",
                "horizontalcontentalignment", "verticalcontentalignment",
                "visibility"
            }
        },
        new()
        {
            DisplayName = "Grid/Dock",
            IsReadOnly = true,
            Properties = new[]
            {
                "grid", "dock"
            }
        },
        new()
        {
            DisplayName = "Color",
            IsReadOnly = true,
            Properties = new[]
            {
                "color", "background", "foreground", "borderbrush", "fill", "stroke"
            }
        },
        new()
        {
            DisplayName = "ItemsControl",
            IsReadOnly = true,
            Properties = new[]
            {
                "items", "itemssource", "selected"
            }
        }
    };

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}