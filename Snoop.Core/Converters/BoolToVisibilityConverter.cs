namespace Snoop.Converters
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;

    public class BoolToVisibilityConverter : IValueConverter, IMultiValueConverter
    {
        public static readonly BoolToVisibilityConverter DefaultInstance = new();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var invert = false;

            if (parameter is string stringParameter)
            {
                bool.TryParse(stringParameter, out invert);
            }

            if (value is bool boolValue)
            {
                return boolValue ^ invert ? Visibility.Visible : Visibility.Collapsed;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = false;

            if (parameter is string stringParameter)
            {
                bool.TryParse(stringParameter, out invert);
            }

            var boolValues = values.OfType<bool>();

            return boolValues.All(x => x) ^ invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}