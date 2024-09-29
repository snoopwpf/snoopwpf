// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using Snoop.Infrastructure;
using Snoop.Windows;

public partial class PropertyGrid2 : INotifyPropertyChanged
{
    public static readonly RoutedCommand ShowBindingErrorsCommand = new(nameof(ShowBindingErrorsCommand), typeof(PropertyGrid2));
    public static readonly RoutedCommand ClearCommand = new(nameof(ClearCommand), typeof(PropertyGrid2));
    public static readonly RoutedCommand SortCommand = new(nameof(SortCommand), typeof(PropertyGrid2));

    public PropertyGrid2()
    {
        this.propertiesView = CollectionViewSource.GetDefaultView(this.Properties);
        this.propertiesView.Filter = item => ((PropertyInformation)item).IsVisible;
        this.Sort(".", ListSortDirection.Ascending);

        this.processIncrementalCall = new DelayedCall(this.ProcessIncrementalPropertyAdd, DispatcherPriority.Background);
        this.filterCall = new DelayedCall(this.propertiesView.Refresh, DispatcherPriority.Background);

        this.InitializeComponent();

        this.Loaded += this.HandleLoaded;
        this.Unloaded += this.HandleUnloaded;

        this.CommandBindings.Add(new CommandBinding(ShowBindingErrorsCommand, this.HandleShowBindingErrors, this.CanShowBindingErrors));
        this.CommandBindings.Add(new CommandBinding(ClearCommand, this.HandleClear, this.CanClear));
        this.CommandBindings.Add(new CommandBinding(SortCommand, this.HandleSort));

        this.filterTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.3)
        };
        this.filterTimer.Tick += (_, _) =>
        {
            this.filterCall.Enqueue(this.Dispatcher);
            this.filterTimer.Stop();
        };
    }

    public bool NameValueOnly
    {
        get
        {
            return this.nameValueOnly;
        }

        set
        {
            this.nameValueOnly = value;
            var gridView = this.ListView is not null && this.ListView.View is not null ? this.ListView.View as GridView : null;
            if (this.nameValueOnly && gridView is not null && gridView.Columns.Count != 2)
            {
                gridView.Columns.RemoveAt(0);
                while (gridView.Columns.Count > 2)
                {
                    gridView.Columns.RemoveAt(2);
                }
            }
        }
    }

    private bool nameValueOnly;

    public ObservableCollection<PropertyInformation> Properties { get; } = new();

    public object? Target
    {
        get { return this.GetValue(TargetProperty); }
        set { this.SetValue(TargetProperty, value); }
    }

    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(object),
            typeof(PropertyGrid2),
            new PropertyMetadata(OnTargetChanged));

    private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var propertyGrid = (PropertyGrid2)d;
        propertyGrid.ChangeTarget(e.NewValue);
    }

    private void ChangeTarget(object newTarget)
    {
        if (this.target != newTarget)
        {
            this.target = newTarget;

            foreach (var property in this.Properties)
            {
                property.Teardown();
            }

            this.RefreshPropertyGrid();

            this.OnPropertyChanged(nameof(this.Type));
        }
    }

    public PropertyInformation? Selection
    {
        get { return this.selection; }

        set
        {
            this.selection = value;
            this.OnPropertyChanged(nameof(this.Selection));
        }
    }

    private PropertyInformation? selection;

    public BindableType? Type
    {
        get
        {
            if (this.target is not null)
            {
                return this.target.GetType();
            }

            return null;
        }
    }

    protected override void OnFilterChanged()
    {
        base.OnFilterChanged();

        this.filterTimer.Stop();
        this.filterTimer.Start();
    }

    /// <summary>
    /// Delayed loading of the property inspector to avoid creating the entire list of property
    /// editors immediately after selection. Keeps that app running smooth.
    /// </summary>
    private void ProcessIncrementalPropertyAdd()
    {
        var numberToAdd = 10;

        if (this.propertiesToAdd is null)
        {
            this.propertiesToAdd = PropertyInformation.GetProperties(this.target).GetEnumerator();

            numberToAdd = 0;
        }

        var i = 0;
        for (; i < numberToAdd && this.propertiesToAdd.MoveNext(); ++i)
        {
            // iterate over the PropertyInfo objects,
            // setting the property grid's filter on each object,
            // and adding those properties to the observable collection of propertiesToSort (this.properties)
            var property = this.propertiesToAdd.Current;
            property.Filter = this.Filter;

            this.Properties.Add(property);
        }

        if (i == numberToAdd)
        {
            this.processIncrementalCall.Enqueue(this.Dispatcher);
        }
        else
        {
            this.propertiesToAdd?.Dispose();
            this.propertiesToAdd = null;
        }
    }

    private void HandleShowBindingErrors(object sender, ExecutedRoutedEventArgs eventArgs)
    {
        var propertyInformation = (PropertyInformation)eventArgs.Parameter;

        if (string.IsNullOrEmpty(propertyInformation.BindingError))
        {
            propertyInformation.UpdateBindingError();
        }

        var textBox = new TextBox
        {
            IsReadOnly = true,
            IsReadOnlyCaretVisible = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };
        textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(propertyInformation.BindingError)) { Source = propertyInformation, Mode = BindingMode.OneWay });

        var window = new SnoopBaseWindow
        {
            Content = textBox,
            Width = 400,
            Height = 300,
            Title = "Binding Errors for " + propertyInformation.DisplayName
        };

        window.Show();
    }

    private void CanShowBindingErrors(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation)
        {
            e.CanExecute = propertyInformation.IsInvalidBinding;
        }

        e.Handled = true;
    }

    private void CanClear(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is PropertyInformation propertyInformation)
        {
            e.CanExecute = propertyInformation.IsLocallySet;
        }

        e.Handled = true;
    }

    private void HandleClear(object sender, ExecutedRoutedEventArgs e)
    {
        ((PropertyInformation)e.Parameter).Clear();
    }

    private ListSortDirection GetNewSortDirection(GridViewColumnHeader columnHeader)
    {
        if (columnHeader.Tag is not ListSortDirection sortDirection)
        {
            return (ListSortDirection)(columnHeader.Tag = ListSortDirection.Ascending);
        }

        return (ListSortDirection)(columnHeader.Tag = (ListSortDirection)(((int)sortDirection + 1) % 2));
    }

    private void HandleSort(object sender, ExecutedRoutedEventArgs args)
    {
        var headerClicked = (GridViewColumnHeader)args.OriginalSource;

        this.direction = this.GetNewSortDirection(headerClicked);
        if (headerClicked.Column is null)
        {
            return;
        }

        var columnHeader = headerClicked.Column.Header as TextBlock;
        if (columnHeader is null)
        {
            return;
        }

        switch (columnHeader.Text)
        {
            case "Name":
                this.Sort(".", this.direction);
                break;
            case "Value":
                this.Sort(nameof(PropertyInformation.StringValue), this.direction);
                break;
            case "Value Source":
                this.Sort(nameof(PropertyInformation.ValueSourceText), this.direction);
                break;
        }
    }

    private void HandleLoaded(object sender, EventArgs e)
    {
        if (this.unloaded)
        {
            this.RefreshPropertyGrid();
            this.unloaded = false;
        }
    }

    private void HandleUnloaded(object sender, EventArgs e)
    {
        foreach (var property in this.Properties)
        {
            property.Teardown();
        }

        this.unloaded = true;
    }

    private void HandleNameClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            var property = (PropertyInformation)((FrameworkElement)sender).DataContext;

            object? newTarget = null;

            if (Keyboard.Modifiers is ModifierKeys.Shift)
            {
                newTarget = property.Binding;
            }
            else if (Keyboard.Modifiers is ModifierKeys.Control)
            {
                newTarget = property.BindingExpression;
            }
            else if (Keyboard.Modifiers is ModifierKeys.None)
            {
                newTarget = property.Value;
            }

            if (newTarget is not null)
            {
                PropertyInspector.DelveCommand.Execute(property, this);
            }
        }
    }

    private void Sort(string propertyPath, ListSortDirection newDirection)
    {
        using (this.propertiesView.DeferRefresh())
        {
            this.propertiesView.SortDescriptions.Clear();
            this.propertiesView.SortDescriptions.Add(new SortDescription(propertyPath, newDirection));
        }
    }

    public void RefreshPropertyGrid()
    {
        this.Properties.Clear();

        this.propertiesToAdd = null;
        this.processIncrementalCall.Enqueue(this.Dispatcher);
    }

    private object? target;

    private IEnumerator<PropertyInformation>? propertiesToAdd;
    private readonly DelayedCall processIncrementalCall;
    private readonly DelayedCall filterCall;
    private bool unloaded;
    private ListSortDirection direction = ListSortDirection.Ascending;

    private readonly DispatcherTimer filterTimer;
    private readonly ICollectionView propertiesView;

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}