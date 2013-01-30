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
            //var viewModel = new FiltersViewModel();
            //viewModel.Filters.Add(new SnoopSingleFilter());
            this.DataContext = viewModel;

            InitializeComponent();
        }

        private void buttonAddFilter_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Filters.Add(new SnoopSingleFilter());
        }

        private void buttonRemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement == null)
                return;

            SnoopFilter filter = frameworkElement.DataContext as SnoopFilter;
            if (filter == null)
                return;

            ViewModel.Filters.Remove(filter);
        }

        private void buttonSetFilter_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.IsSet = true;
            this.Close();
        } 
    }

    public class FiltersViewModel : INotifyPropertyChanged
    {
        public FiltersViewModel()
        {
            this.Filters = new ObservableCollection<SnoopFilter>();
            Filters.Add(new SnoopSingleFilter());
        }

        public void ClearFilters()
        {
            Filters.Clear();
            Filters.Add(new SnoopSingleFilter());
        }

        public bool FilterMatches(string str)
        {
            foreach (var filter in Filters)
            {
                if (filter.FilterMatches(str))
                    return true;
            }
            return false;
        }

        public bool IsSet { get; set; }
       
        public ObservableCollection<SnoopFilter> Filters { get; private set; }

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

    public abstract class SnoopFilter : INotifyPropertyChanged
    {
        public abstract bool FilterMatches(string debugLine);

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
