using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public class SnoopMultipleFilter : SnoopFilter
	{
		private List<SnoopFilter> _singleFilters = new List<SnoopFilter>();

		public override bool FilterMatches(string debugLine)
		{
			foreach (var filter in _singleFilters)
			{
				if (!filter.FilterMatches(debugLine))
					return false;
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
				if (_singleFilters.Count == 0)
					return string.Empty;

				return _singleFilters[0].GroupId;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public bool IsValidMultipleFilter
		{
			get
			{
				return _singleFilters.Count > 0;
			}
		}

		public void AddFilter(SnoopFilter singleFilter)
		{
			if (!singleFilter.SupportsGrouping)
				throw new NotSupportedException("The filter is not grouped");
			_singleFilters.Add(singleFilter);
		}

		public void RemoveFilter(SnoopFilter singleFilter)
		{
			singleFilter.IsGrouped = false;
			_singleFilters.Remove(singleFilter);
		}

		public void AddRange(IEnumerable<SnoopFilter> filters, string groupID)
		{
			foreach (var filter in filters)
			{
				if (!filter.SupportsGrouping)
					throw new NotSupportedException("The filter is not grouped");

				filter.IsGrouped = true;
				filter.GroupId = groupID;
			}
			_singleFilters.AddRange(filters);
		}

		public void ClearFilters()
		{
			foreach (var filter in _singleFilters)
				filter.IsGrouped = false;
			_singleFilters.Clear();
		}

		public bool ContainsFilter(SnoopSingleFilter filter)
		{
			return _singleFilters.Contains(filter);
		}
	}
}
