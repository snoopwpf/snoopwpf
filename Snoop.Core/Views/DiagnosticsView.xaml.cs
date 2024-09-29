namespace Snoop.Views;

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Snoop.Infrastructure.Diagnostics;

public partial class DiagnosticsView
{
    public static readonly DependencyProperty DiagnosticContextProperty =
        DependencyProperty.Register(nameof(DiagnosticContext), typeof(DiagnosticContext), typeof(DiagnosticsView), new PropertyMetadata(default(DiagnosticContext), OnDiagnosticContextChanged));

    public static readonly DependencyProperty DiagnosticProvidersProperty = DependencyProperty.Register(
        nameof(DiagnosticProviders), typeof(ObservableCollection<DiagnosticProvider>), typeof(DiagnosticsView), new PropertyMetadata(null));

    public static readonly DependencyProperty DiagnosticProvidersViewProperty = DependencyProperty.Register(
        nameof(DiagnosticProvidersView), typeof(ICollectionView), typeof(DiagnosticsView), new PropertyMetadata(default(ICollectionView)));

    public static readonly DependencyProperty DiagnosticItemsProperty = DependencyProperty.Register(
        nameof(DiagnosticItems), typeof(ObservableCollection<DiagnosticItem>), typeof(DiagnosticsView), new PropertyMetadata(null));

    public static readonly DependencyProperty DiagnosticsItemsViewProperty = DependencyProperty.Register(
        nameof(DiagnosticsItemsView), typeof(ICollectionView), typeof(DiagnosticsView), new PropertyMetadata(default(ICollectionView)));

    public static readonly DependencyProperty ShowErrorsProperty = DependencyProperty.Register(
        nameof(ShowErrors), typeof(bool), typeof(DiagnosticsView), new PropertyMetadata(true, OnFilterPropertyChanged));

    public static readonly DependencyProperty ShowWarningsProperty = DependencyProperty.Register(
        nameof(ShowWarnings), typeof(bool), typeof(DiagnosticsView), new PropertyMetadata(true, OnFilterPropertyChanged));

    public static readonly DependencyProperty ShowInformationsProperty = DependencyProperty.Register(
        nameof(ShowInformations), typeof(bool), typeof(DiagnosticsView), new PropertyMetadata(true, OnFilterPropertyChanged));

    public static readonly DependencyProperty ErrorCountProperty = DependencyProperty.Register(
        nameof(ErrorCount), typeof(int), typeof(DiagnosticsView), new PropertyMetadata(default(int)));

    public static readonly DependencyProperty WarningCountProperty = DependencyProperty.Register(
        nameof(WarningCount), typeof(int), typeof(DiagnosticsView), new PropertyMetadata(default(int)));

    public static readonly DependencyProperty InformationCountProperty = DependencyProperty.Register(
        nameof(InformationCount), typeof(int), typeof(DiagnosticsView), new PropertyMetadata(default(int)));

    public static readonly RoutedCommand ResetEnabledDiagnosticsToDefaultCommand = new(nameof(ResetEnabledDiagnosticsToDefaultCommand), typeof(DiagnosticsView));

    public DiagnosticsView()
    {
        this.InitializeComponent();

        this.CommandBindings.Add(new CommandBinding(ResetEnabledDiagnosticsToDefaultCommand, this.HandleResetResetEnabledDiagnosticsToDefault));
    }

    public ObservableCollection<DiagnosticProvider>? DiagnosticProviders
    {
        get => (ObservableCollection<DiagnosticProvider>?)this.GetValue(DiagnosticProvidersProperty);
        set => this.SetValue(DiagnosticProvidersProperty, value);
    }

    public ICollectionView? DiagnosticProvidersView
    {
        get => (ICollectionView?)this.GetValue(DiagnosticProvidersViewProperty);
        set => this.SetValue(DiagnosticProvidersViewProperty, value);
    }

    public ObservableCollection<DiagnosticItem>? DiagnosticItems
    {
        get => (ObservableCollection<DiagnosticItem>?)this.GetValue(DiagnosticItemsProperty);
        set => this.SetValue(DiagnosticItemsProperty, value);
    }

    public int ErrorCount
    {
        get => (int)this.GetValue(ErrorCountProperty);
        set => this.SetValue(ErrorCountProperty, value);
    }

    public int WarningCount
    {
        get => (int)this.GetValue(WarningCountProperty);
        set => this.SetValue(WarningCountProperty, value);
    }

    public int InformationCount
    {
        get => (int)this.GetValue(InformationCountProperty);
        set => this.SetValue(InformationCountProperty, value);
    }

    public ICollectionView? DiagnosticsItemsView
    {
        get => (ICollectionView?)this.GetValue(DiagnosticsItemsViewProperty);
        set => this.SetValue(DiagnosticsItemsViewProperty, value);
    }

    public DiagnosticContext? DiagnosticContext
    {
        get => (DiagnosticContext?)this.GetValue(DiagnosticContextProperty);
        set => this.SetValue(DiagnosticContextProperty, value);
    }

    public bool ShowErrors
    {
        get => (bool)this.GetValue(ShowErrorsProperty);
        set => this.SetValue(ShowErrorsProperty, value);
    }

    public bool ShowWarnings
    {
        get => (bool)this.GetValue(ShowWarningsProperty);
        set => this.SetValue(ShowWarningsProperty, value);
    }

    public bool ShowInformations
    {
        get => (bool)this.GetValue(ShowInformationsProperty);
        set => this.SetValue(ShowInformationsProperty, value);
    }

    private void HandleResetResetEnabledDiagnosticsToDefault(object sender, ExecutedRoutedEventArgs e)
    {
        if (this.DiagnosticProviders is null)
        {
            return;
        }

        foreach (var diagnosticProvider in this.DiagnosticProviders)
        {
            diagnosticProvider.IsActive = true;
        }
    }

    private static void OnDiagnosticContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DiagnosticsView)d;

        if (control.DiagnosticItems is not null)
        {
            ((INotifyCollectionChanged)control.DiagnosticItems).CollectionChanged -= control.OnCollectionChanged;
        }

        control.DiagnosticItems = ((DiagnosticContext?)e.NewValue)?.DiagnosticItems;

        if (control.DiagnosticItems is not null)
        {
            ((INotifyCollectionChanged)control.DiagnosticItems).CollectionChanged += control.OnCollectionChanged;

            var newView = new ListCollectionView(control.DiagnosticItems)
            {
                Filter = control.FilterDiagnosticItems,
                SortDescriptions = { new SortDescription(nameof(DiagnosticItem.Level), ListSortDirection.Descending) }
            };
            control.DiagnosticsItemsView = newView;
        }
        else
        {
            control.DiagnosticsItemsView = null;
        }

        control.DiagnosticProviders = ((DiagnosticContext?)e.NewValue)?.DiagnosticProviders;

        if (control.DiagnosticProviders is not null)
        {
            var newView = new ListCollectionView(control.DiagnosticProviders);

            // newView.GroupDescriptions.Add(new
            // {
            //     PropertyName = nameof(DiagnosticProvider.Category),
            //     StringComparison = StringComparison.OrdinalIgnoreCase
            // });

            // cvs.SortDescriptions.Add(new SortDescription(nameof(DiagnosticProvider.Category), ListSortDirection.Ascending));
            newView.SortDescriptions.Add(new SortDescription(nameof(DiagnosticProvider.Name), ListSortDirection.Ascending));

            control.DiagnosticProvidersView = newView;
        }
        else
        {
            control.DiagnosticProvidersView = null;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.ErrorCount = this.DiagnosticItems!.Count(x => x.Level is DiagnosticLevel.Critical or DiagnosticLevel.Error);
        this.WarningCount = this.DiagnosticItems!.Count(x => x.Level == DiagnosticLevel.Warning);
        this.InformationCount = this.DiagnosticItems!.Count(x => x.Level == DiagnosticLevel.Info);
    }

    private bool FilterDiagnosticItems(object obj)
    {
        if (obj is not DiagnosticItem diagnosticItem)
        {
            return false;
        }

        if (diagnosticItem.DiagnosticProvider?.IsActive == false)
        {
            return false;
        }

        return diagnosticItem.Level switch
        {
            DiagnosticLevel.Info => this.ShowInformations,
            DiagnosticLevel.Warning => this.ShowWarnings,
            DiagnosticLevel.Error => this.ShowErrors,
            DiagnosticLevel.Critical => true,
#pragma warning disable CA2208
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticItem.Level), diagnosticItem.Level, "Unknown diagnostic level.")
#pragma warning restore CA2208
        };
    }

    private static void OnFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DiagnosticsView)d;
        control.DiagnosticsItemsView?.Refresh();
    }

    private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        this.NavigateToSelectedTreeItem();
    }

    private void Diagnostics_OnKeyUp(object sender, KeyEventArgs e)
    {
        this.NavigateToSelectedTreeItem();
    }

    private void NavigateToSelectedTreeItem()
    {
        var diagnosticItem = this.diagnostics.SelectedItem as DiagnosticItem;

        if (diagnosticItem?.TreeItem != null)
        {
            diagnosticItem.TreeItem.IsSelected = true;
        }
    }
}