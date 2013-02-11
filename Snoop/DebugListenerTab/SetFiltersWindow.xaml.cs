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
using System.Runtime.Serialization.Formatters.Binary;

namespace Snoop.DebugListenerTab
{
    /// <summary>
    /// Interaction logic for SetFiltersWindow.xaml
    /// </summary>
    public partial class SetFiltersWindow : Window
    {
        internal FiltersViewModel ViewModel
        {
            get
            {
                return this.DataContext as FiltersViewModel;
            }

        }


        private List<SnoopSingleFilter> initialFilters;
        public SetFiltersWindow(FiltersViewModel viewModel)
        {
            this.DataContext = viewModel;

            InitializeComponent();
            initialFilters = MakeDeepCopyOfFilters(this.ViewModel.Filters); 
            this.Closing += SetFiltersWindow_Closing;
        }

        void SetFiltersWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_setFilterClicked)
                return;

            var saveChanges = MessageBox.Show("Save changes?", "Changes", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            if (saveChanges)
                return;

            this.ViewModel.InitializeFilters(initialFilters);
        }


        private void buttonAddFilter_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.Filters.Add(new SnoopSingleFilter());
            ViewModel.AddFilter(new SnoopSingleFilter());
            //this.listBoxFilters.ScrollIntoView(this.listBoxFilters.ItemContainerGenerator.ContainerFromIndex(this.listBoxFilters.Items.Count - 1));

        }

        private void buttonRemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement == null)
                return;

            SnoopFilter filter = frameworkElement.DataContext as SnoopFilter;
            if (filter == null)
                return;

            ViewModel.RemoveFilter(filter);
        }
        private bool _setFilterClicked = false;
        private void buttonSetFilter_Click(object sender, RoutedEventArgs e)
        {
            SaveFiltersToSettings();

            //this.ViewModel.IsSet = true;
            this.ViewModel.SetIsSet();
            _setFilterClicked = true;
            this.Close();
        }

        private void SaveFiltersToSettings()
        {
            List<SnoopSingleFilter> singleFilters = new List<SnoopSingleFilter>();
            foreach (var filter in this.ViewModel.Filters)
            {
                if (filter is SnoopSingleFilter)
                    singleFilters.Add((SnoopSingleFilter)filter);
            }

            Properties.Settings.Default.SnoopDebugFilters = singleFilters.ToArray();
        }

        private void textBlockFilter_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Focus();
                this.listBoxFilters.ScrollIntoView(textBox);
            }
        }

        private void menuItemGroupFilters_Click(object sender, RoutedEventArgs e)
        {

            List<SnoopFilter> filtersToGroup = new List<SnoopFilter>();
            foreach (var item in this.listBoxFilters.SelectedItems)
            {
                var filter = item as SnoopFilter;
                if (filter == null)
                    continue;

                if (filter.SupportsGrouping)
                    filtersToGroup.Add(filter);
            }
            this.ViewModel.GroupFilters(filtersToGroup);
        }

        private void menuItemClearFilterGroups_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.ClearFilterGroups();
        }        

        private List<SnoopSingleFilter> MakeDeepCopyOfFilters(IEnumerable<SnoopFilter> filters)
        {
            List<SnoopSingleFilter> snoopSingleFilters = new List<SnoopSingleFilter>();

            foreach (var filter in filters)
            {
                var singleFilter = filter as SnoopSingleFilter;
                if (singleFilter == null)
                    continue;

                var newFilter = (SnoopSingleFilter)singleFilter.Clone();

                snoopSingleFilters.Add(newFilter);
            }

            return snoopSingleFilters;
        }

        //private SnoopSingleFilter MakeDeepCopyOfFilter(SnoopSingleFilter filter)
        //{
        //    try
        //    {
        //        BinaryFormatter formatter = new BinaryFormatter();
        //        var ms = new System.IO.MemoryStream();
        //        formatter.Serialize(ms, filter);
        //        SnoopSingleFilter deepCopy = (SnoopSingleFilter)formatter.Deserialize(ms);
        //        ms.Close();
        //        return deepCopy;
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}
    }

  
    [Serializable]
    public class FiltersViewModel : INotifyPropertyChanged
    {
        private List<SnoopMultipleFilter> multipleFilters = new List<SnoopMultipleFilter>();

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

            //var groupings = singleFilters.Select(x => x.IsGrouped).GroupBy(x => x.GroupId);
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
            //Filters.Clear();
            //Filters.Add(new SnoopSingleFilter());
            filters.Clear();
            filters.Add(new SnoopSingleFilter());
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

            foreach(var multipleFilter in this.multipleFilters)
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

            this.filters.Add(filter);
        }

        public void RemoveFilter(SnoopFilter filter)
        {
            var singleFilter = filter as SnoopSingleFilter;
            if (singleFilter != null)
            {
                foreach (var multipeFilter in this.multipleFilters)
                {
                    if (multipeFilter.ContainsFilter(singleFilter))
                        multipeFilter.RemoveFilter(singleFilter);
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

    public enum FilterType
    {
        StartsWith,
        EndsWith,
        Contains,
        RegularExpression
    }

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

    [Serializable]
    public abstract class SnoopFilter : INotifyPropertyChanged
    {
        protected bool _isGrouped = false;
        protected string _groupId = string.Empty;

        public abstract bool FilterMatches(string debugLine);

        public virtual bool SupportsGrouping
        {
            get
            {
                return true;
            }
        }

        public bool IsGrouped
        {
            get
            {
                return _isGrouped;
            }
            set
            {
                _isGrouped = value;
                this.RaisePropertyChanged("IsGrouped");
                GroupId = string.Empty;
            }
        }

        public virtual string GroupId
        {
            get
            {
                return _groupId;
            }
            set
            {
                _groupId = value;
                this.RaisePropertyChanged("GroupId");
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

    [Serializable]
    public class SnoopSingleFilter : SnoopFilter, ICloneable
    {
        private string _text;

        public SnoopSingleFilter()
        {
            this.Text = string.Empty;
        }

        public FilterType FilterType { get; set; }
        
        
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                this.RaisePropertyChanged("Text");
            }
        }

        public override bool FilterMatches(string debugLine)
        {
            debugLine = debugLine.ToLower();
            var text = Text.ToLower();
            switch (FilterType)
            {
                case DebugListenerTab.FilterType.Contains:
                    return debugLine.Contains(text);
                case DebugListenerTab.FilterType.StartsWith:
                    return debugLine.StartsWith(text);
                case DebugListenerTab.FilterType.EndsWith:
                    return debugLine.EndsWith(text);
                case DebugListenerTab.FilterType.RegularExpression:
                    return Regex.IsMatch(debugLine, text);
            }
            return false;
        }

        public object Clone()
        {
            SnoopSingleFilter newFilter = new SnoopSingleFilter();
            newFilter._groupId = this._groupId;
            newFilter._isGrouped = this._isGrouped;
            newFilter._text = this._text;
            newFilter.FilterType = this.FilterType;

            return newFilter;
        }
    }
}

namespace Snoop.Converters
{
    using Snoop.DebugListenerTab;

    public class FilterTypeToIntConverter : IValueConverter
    {
        public static readonly FilterTypeToIntConverter Default = new FilterTypeToIntConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is FilterType))
                return value;

            FilterType filterType = (FilterType)value;
            return (int)filterType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int))
                return value;

            int intValue = (int)value;
            return (FilterType)intValue;
        }
    }
}
