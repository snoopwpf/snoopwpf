namespace Snoop.Views.DebugListenerTab;

using System;
using System.Collections.Generic;

[Serializable]
public class SnoopMultipleFilter : SnoopFilter
{
    private readonly List<SnoopFilter> singleFilters = new();

    public override bool FilterMatches(string? debugLine)
    {
        foreach (var filter in this.singleFilters)
        {
            if (!filter.FilterMatches(debugLine))
            {
                return false;
            }
        }

        return true;
    }

    public override bool SupportsGrouping
    {
        get
        {
            return false;
        }
    }

    public override string GroupId
    {
        get
        {
            if (this.singleFilters.Count == 0)
            {
                return string.Empty;
            }

            return this.singleFilters[0].GroupId;
        }

#pragma warning disable INPC021
        set
        {
            throw new NotSupportedException();
        }
#pragma warning restore INPC021
    }

    public bool IsValidMultipleFilter
    {
        get
        {
            return this.singleFilters.Count > 0;
        }
    }

    public void AddFilter(SnoopFilter singleFilter)
    {
        if (!singleFilter.SupportsGrouping)
        {
            throw new NotSupportedException("The filter is not grouped");
        }

        this.singleFilters.Add(singleFilter);
    }

    public void RemoveFilter(SnoopFilter singleFilter)
    {
        singleFilter.IsGrouped = false;
        this.singleFilters.Remove(singleFilter);
    }

    public void AddRange(IEnumerable<SnoopFilter> filters, string groupID)
    {
        foreach (var filter in filters)
        {
            if (!filter.SupportsGrouping)
            {
                throw new NotSupportedException("The filter is not grouped");
            }

            filter.IsGrouped = true;
            filter.GroupId = groupID;
        }

        this.singleFilters.AddRange(filters);
    }

    public void ClearFilters()
    {
        foreach (var filter in this.singleFilters)
        {
            filter.IsGrouped = false;
        }

        this.singleFilters.Clear();
    }

    public bool ContainsFilter(SnoopSingleFilter filter)
    {
        return this.singleFilters.Contains(filter);
    }
}