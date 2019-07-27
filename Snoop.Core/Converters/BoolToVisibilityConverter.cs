namespace Snoop.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter DefaultInstance = new BoolToVisibilityConverter();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool invert = false;

            if (parameter is string)
            {
                bool.TryParse((string)parameter, out invert);
            }

            if( value is bool)
            {
                return ((bool)value) ^ invert ? Visibility.Visible : Visibility.Collapsed;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        #endregion
    }
}