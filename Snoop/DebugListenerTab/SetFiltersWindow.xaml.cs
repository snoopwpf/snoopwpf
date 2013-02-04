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

        public SetFiltersWindow(FiltersViewModel viewModel)
        {
            this.DataContext = viewModel;

            InitializeComponent();

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

            //ViewModel.Filters.Remove(filter);
            ViewModel.RemoveFilter(filter);
           
        }

        private void buttonSetFilter_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.IsSet = true;
            this.Close();
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
    }

    public class FiltersViewModel : INotifyPropertyChanged
    {
        private List<SnoopMultipleFilter> multipleFilters = new List<SnoopMultipleFilter>();

        public FiltersViewModel()
        {
            //this.Filters = new ObservableCollection<SnoopFilter>();
            //Filters.Add(new SnoopSingleFilter());
            filters.Add(new SnoopSingleFilter());
        }

        public void ClearFilters()
        {
            //Filters.Clear();
            //Filters.Add(new SnoopSingleFilter());
            filters.Clear();
            filters.Add(new SnoopSingleFilter());
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

        public void GroupFilters(IEnumerable<SnoopFilter> filtersToGroup)
        {
            SnoopMultipleFilter multipleFilter = new SnoopMultipleFilter();
            multipleFilter.AddRange(filtersToGroup);

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

        public bool IsSet { get; set; }

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

        public void AddRange(IEnumerable<SnoopFilter> filters)
        {
            foreach (var filter in filters)
            {
                if (!filter.SupportsGrouping)
                    throw new NotSupportedException("The filter is not grouped");

                filter.IsGrouped = true;
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

    public abstract class SnoopFilter : INotifyPropertyChanged
    {
        private bool _isGrouped;

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

    public class SnoopSingleFilter : SnoopFilter
    {
        public SnoopSingleFilter()
        {
            this.Text = string.Empty;
        }

        public FilterType FilterType { get; set; }
        private string _text;
        
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
