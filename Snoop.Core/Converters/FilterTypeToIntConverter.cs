namespace Snoop.Converters
{
    using System;
    using System.Windows.Data;
    using Snoop.DebugListenerTab;

    public class FilterTypeToIntConverter : IValueConverter
    {
        public static readonly FilterTypeToIntConverter Default = new FilterTypeToIntConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is FilterType))
            {
                return value;
            }

            var filterType = (FilterType)value;
            return (int)filterType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int))
            {
                return value;
            }

            var intValue = (int)value;
            return (FilterType)intValue;
        }
    }
}
