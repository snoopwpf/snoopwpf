namespace Snoop.Views.DebugListenerTab;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;

[Serializable]
public class FiltersViewModel : INotifyPropertyChanged
{
    private readonly List<SnoopMultipleFilter> multipleFilters = new();
    private bool isDirty;

    public void ResetDirtyFlag()
    {
        this.IsDirty = false;

        foreach (var filter in this.filters)
        {
            filter.ResetDirtyFlag();
        }
    }

    public bool IsDirty
    {
        get
        {
            if (this.isDirty)
            {
                return true;
            }

            foreach (var filter in this.filters)
            {
                if (filter.IsDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private set
        {
            this.isDirty = value;

            this.RaisePropertyChanged(nameof(this.IsDirty));
        }
    }

    public FiltersViewModel()
    {
        this.InitializeFilters(null);
    }

    public FiltersViewModel(IList<SnoopSingleFilter>? singleFilters)
    {
        this.InitializeFilters(singleFilters);
    }

    public void InitializeFilters(IList<SnoopSingleFilter>? singleFilters)
    {
        this.filters.Clear();

        if (singleFilters is null)
        {
            this.filters.Add(new SnoopSingleFilter());
            this.IsSet = false;
            return;
        }

        foreach (var filter in singleFilters)
        {
            this.filters.Add(filter);
        }

        var groupings = (from x in singleFilters where x.IsGrouped select x).GroupBy(x => x.GroupId);
        foreach (var grouping in groupings)
        {
            var multipleFilter = new SnoopMultipleFilter();
            var groupedFilters = grouping.ToArray();
            if (groupedFilters.Length == 0)
            {
                continue;
            }

            multipleFilter.AddRange(groupedFilters, groupedFilters[0].GroupId);
            this.multipleFilters.Add(multipleFilter);
        }

        this.SetIsSet();
    }

    internal void SetIsSet()
    {
        if (this.filters.Count == 1 && this.filters[0] is SnoopSingleFilter && string.IsNullOrEmpty(((SnoopSingleFilter)this.filters[0]).Text))
        {
            this.IsSet = false;
        }
        else
        {
            this.IsSet = true;
        }
    }

    public void ClearFilters()
    {
        this.multipleFilters.Clear();
        this.filters.Clear();
        this.filters.Add(new SnoopSingleFilter());
        this.IsSet = false;
    }

    public bool FilterMatches(string? str)
    {
        foreach (var filter in this.Filters)
        {
            if (filter.IsGrouped)
            {
                continue;
            }

            if (filter.FilterMatches(str))
            {
                return true;
            }
        }

        foreach (var multipleFilter in this.multipleFilters)
        {
            if (multipleFilter.FilterMatches(str))
            {
                return true;
            }
        }

        return this.filters.Count is 0
            && this.multipleFilters.Count is 0;
    }

    private string GetFirstNonUsedGroupId()
    {
        var index = 1;
        while (true)
        {
            if (!this.GroupIdTaken(index.ToString()))
            {
                return index.ToString();
            }

            index++;
        }
    }

    private bool GroupIdTaken(string groupID)
    {
        foreach (var filter in this.multipleFilters)
        {
            if (groupID.Equals(filter.GroupId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public void GroupFilters(IEnumerable<SnoopFilter> filtersToGroup)
    {
        var multipleFilter = new SnoopMultipleFilter();
        multipleFilter.AddRange(filtersToGroup, (this.multipleFilters.Count + 1).ToString());

        this.multipleFilters.Add(multipleFilter);
    }

    public void AddFilter(SnoopFilter filter)
    {
        this.IsDirty = true;

        this.filters.Add(filter);
    }

    public void RemoveFilter(SnoopFilter filter)
    {
        this.IsDirty = true;

        var singleFilter = filter as SnoopSingleFilter;
        if (singleFilter is not null)
        {
            //foreach (var multipleFilter in this.multipleFilters)
            var index = 0;
            while (index < this.multipleFilters.Count)
            {
                var multipeFilter = this.multipleFilters[index];
                if (multipeFilter.ContainsFilter(singleFilter))
                {
                    multipeFilter.RemoveFilter(singleFilter);
                }

                if (!multipeFilter.IsValidMultipleFilter)
                {
                    this.multipleFilters.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        this.filters.Remove(filter);
    }

    public void ClearFilterGroups()
    {
        foreach (var filterGroup in this.multipleFilters)
        {
            filterGroup.ClearFilters();
        }

        this.multipleFilters.Clear();
    }

    private bool isSet;
    private string filterStatus = "Filter is OFF";

    public bool IsSet
    {
        get
        {
            return this.isSet;
        }

        set
        {
            this.isSet = value;
            this.RaisePropertyChanged(nameof(this.IsSet));
            this.FilterStatus = this.isSet ? "Filter is ON" : "Filter is OFF";
        }
    }

    public string FilterStatus
    {
        get
        {
            return this.filterStatus;
        }

        set
        {
            this.filterStatus = value;
            this.RaisePropertyChanged(nameof(this.FilterStatus));
        }
    }

    private readonly ObservableCollection<SnoopFilter> filters = new();

    public IEnumerable<SnoopFilter> Filters => this.filters;

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void RaisePropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}