namespace Snoop.Views.DebugListenerTab;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Snoop.Core;

public partial class SetFiltersWindow
{
    public SetFiltersWindow(FiltersViewModel viewModel)
    {
        this.DataContext = viewModel;
        viewModel.ResetDirtyFlag();

        this.InitializeComponent();

        this.initialFilters = this.MakeDeepCopyOfFilters(this.ViewModel.Filters);

        this.Closed += this.SetFiltersWindow_Closed;
    }

    internal FiltersViewModel ViewModel => (FiltersViewModel)this.DataContext;

    private void SetFiltersWindow_Closed(object? sender, EventArgs e)
    {
        if (this.setFilterClicked || !this.ViewModel.IsDirty)
        {
            return;
        }

        var saveChanges = MessageBox.Show("Save changes?", "Changes", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        if (saveChanges)
        {
            this.ViewModel.SetIsSet();
            this.SaveFiltersToSettings();
            return;
        }

        this.ViewModel.InitializeFilters(this.initialFilters);
    }

    private void ButtonAddFilter_Click(object sender, RoutedEventArgs e)
    {
        //ViewModel.Filters.Add(new SnoopSingleFilter());
        this.ViewModel.AddFilter(new SnoopSingleFilter());
        //this.listBoxFilters.ScrollIntoView(this.listBoxFilters.ItemContainerGenerator.ContainerFromIndex(this.listBoxFilters.Items.Count - 1));
    }

    private void ButtonRemoveFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SnoopFilter filter })
        {
            this.ViewModel.RemoveFilter(filter);
        }
    }

    private void ButtonSetFilter_Click(object sender, RoutedEventArgs e)
    {
        this.SaveFiltersToSettings();

        //this.ViewModel.IsSet = true;
        this.ViewModel.SetIsSet();
        this.setFilterClicked = true;
        this.Close();
    }

    private void TextBlockFilter_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus();
            this.listBoxFilters.ScrollIntoView(textBox);
        }
    }

    private void MenuItemGroupFilters_Click(object sender, RoutedEventArgs e)
    {
        var filtersToGroup = new List<SnoopFilter>();
        foreach (var item in this.listBoxFilters.SelectedItems)
        {
            var filter = item as SnoopFilter;
            if (filter is null)
            {
                continue;
            }

            if (filter.SupportsGrouping)
            {
                filtersToGroup.Add(filter);
            }
        }

        this.ViewModel.GroupFilters(filtersToGroup);
    }

    private void MenuItemClearFilterGroups_Click(object sender, RoutedEventArgs e)
    {
        this.ViewModel.ClearFilterGroups();
    }

    private void MenuItemSetInverse_Click(object sender, RoutedEventArgs e)
    {
        foreach (SnoopFilter? filter in this.listBoxFilters.SelectedItems)
        {
            if (filter is null)
            {
                continue;
            }

            filter.IsInverse = !filter.IsInverse;
        }
    }

    private void SaveFiltersToSettings()
    {
        var singleFilters = new List<SnoopSingleFilter>();
        foreach (var filter in this.ViewModel.Filters)
        {
            if (filter is SnoopSingleFilter)
            {
                singleFilters.Add((SnoopSingleFilter)filter);
            }
        }

        Settings.Default.SnoopDebugFilters.UpdateWith(singleFilters.ToArray());
    }

    private List<SnoopSingleFilter> MakeDeepCopyOfFilters(IEnumerable<SnoopFilter> filters)
    {
        var snoopSingleFilters = new List<SnoopSingleFilter>();

        foreach (var filter in filters)
        {
            var singleFilter = filter as SnoopSingleFilter;
            if (singleFilter is null)
            {
                continue;
            }

            var newFilter = (SnoopSingleFilter)singleFilter.Clone();

            snoopSingleFilters.Add(newFilter);
        }

        return snoopSingleFilters;
    }

    //private SnoopSingleFilter MakeDeepCopyOfFilter(SnoopSingleFilter filter)
    //{
    //  try
    //  {
    //      BinaryFormatter formatter = new BinaryFormatter();
    //      var ms = new System.IO.MemoryStream();
    //      formatter.Serialize(ms, filter);
    //      SnoopSingleFilter deepCopy = (SnoopSingleFilter)formatter.Deserialize(ms);
    //      ms.Close();
    //      return deepCopy;
    //  }
    //  catch (Exception)
    //  {
    //      return null;
    //  }
    //}

    private readonly List<SnoopSingleFilter> initialFilters;
    private bool setFilterClicked;
}