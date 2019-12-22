namespace Snoop.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class NullStyleConverter : IValueConverter
    {
        public static readonly NullStyleConverter DefaultInstance = new NullStyleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return value;
            }

            if (parameter is FrameworkElement fe)
            {
                return fe.TryFindResource(fe.GetType());
            }

            if (parameter is FrameworkContentElement fec)
            {
                return fec.TryFindResource(fec.GetType());
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}