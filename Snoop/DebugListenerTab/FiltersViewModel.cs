using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public class FiltersViewModel : INotifyPropertyChanged
	{
		private List<SnoopMultipleFilter> multipleFilters = new List<SnoopMultipleFilter>();
		private bool isDirty = false;

		public void ResetDirtyFlag()
		{
			isDirty = false;
			foreach (var filter in this.filters)
			{
				filter.ResetDirtyFlag();
			}
		}

		public bool IsDirty
		{
			get
			{
				if (isDirty)
					return true;

				foreach (var filter in this.filters)
				{
					if (filter.IsDirty)
						return true;
				}
				return false;
			}
		}

		public FiltersViewModel()
		{
			filters.Add(new SnoopSingleFilter());
			FilterStatus = _isSet ? "Filter is ON" : "Filter is OFF";
		}

		public FiltersViewModel(IList<SnoopSingleFilter> singleFilters)
		{
			InitializeFilters(singleFilters);
		}

		public void InitializeFilters(IList<SnoopSingleFilter> singleFilters)
		{
			this.filters.Clear();

			if (singleFilters == null)
			{
				filters.Add(new SnoopSingleFilter());
				this.IsSet = false;
				return;
			}

			foreach (var filter in singleFilters)
				this.filters.Add(filter);

			var groupings = (from x in singleFilters where x.IsGrouped select x).GroupBy(x => x.GroupId);
			foreach (var grouping in groupings)
			{
				var multipleFilter = new SnoopMultipleFilter();
				var groupedFilters = grouping.ToArray();
				if (groupedFilters.Length == 0)
					continue;

				multipleFilter.AddRange(groupedFilters, groupedFilters[0].GroupId);
				this.multipleFilters.Add(multipleFilter);
			}

			SetIsSet();
		}

		internal void SetIsSet()
		{
			if (filters == null)
				this.IsSet = false;

			if (filters.Count == 1 && filters[0] is SnoopSingleFilter && string.IsNullOrEmpty(((SnoopSingleFilter)filters[0]).Text))
				this.IsSet = false;
			else
				this.IsSet = true;
		}

		public void ClearFilters()
		{
			this.multipleFilters.Clear();
			this.filters.Clear();
			this.filters.Add(new SnoopSingleFilter());
			this.IsSet = false;
		}

		public bool FilterMatches(string str)
		{
			foreach (var filter in Filters)
			{
				if (filter.IsGrouped)
					continue;

				if (filter.FilterMatches(str))
					return true;
			}

			foreach (var multipleFilter in this.multipleFilters)
			{
				if (multipleFilter.FilterMatches(str))
					return true;
			}

			return false;
		}

		private string GetFirstNonUsedGroupId()
		{
			int index = 1;
			while (true)
			{
				if (!GroupIdTaken(index.ToString()))
					return index.ToString();

				index++;
			}
		}

		private bool GroupIdTaken(string groupID)
		{
			foreach (var filter in multipleFilters)
			{
				if (groupID.Equals(filter.GroupId))
					return true;
			}
			return false;
		}

		public void GroupFilters(IEnumerable<SnoopFilter> filtersToGroup)
		{
			SnoopMultipleFilter multipleFilter = new SnoopMultipleFilter();
			multipleFilter.AddRange(filtersToGroup, (multipleFilters.Count + 1).ToString());

			multipleFilters.Add(multipleFilter);
		}

		public void AddFilter(SnoopFilter filter)
		{
			isDirty = true;
			this.filters.Add(filter);
		}

		public void RemoveFilter(SnoopFilter filter)
		{
			isDirty = true;
			var singleFilter = filter as SnoopSingleFilter;
			if (singleFilter != null)
			{
				//foreach (var multipeFilter in this.multipleFilters)
				int index = 0;
				while (index < this.multipleFilters.Count)
				{
					var multipeFilter = this.multipleFilters[index];
					if (multipeFilter.ContainsFilter(singleFilter))
						multipeFilter.RemoveFilter(singleFilter);

					if (!multipeFilter.IsValidMultipleFilter)
						this.multipleFilters.RemoveAt(index);
					else
						index++;
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

		private bool _isSet;
		private string _filterStatus;
		public bool IsSet
		{
			get
			{
				return _isSet;
			}
			set
			{
				_isSet = value;
				RaisePropertyChanged("IsSet");
				FilterStatus = _isSet ? "Filter is ON" : "Filter is OFF";
			}
		}

		public string FilterStatus
		{
			get
			{
				return _filterStatus;
			}
			set
			{
				_filterStatus = value;
				RaisePropertyChanged("FilterStatus");
			}
		}

		private ObservableCollection<SnoopFilter> filters = new ObservableCollection<SnoopFilter>();
		public IEnumerable<SnoopFilter> Filters
		{
			get
			{
				return filters;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string propertyName)
		{
			var handler = this.PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
