namespace Snoop.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class NullStyleConverter : IValueConverter
    {
        public static readonly NullStyleConverter DefaultInstance = new();

        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not null)
            {
                return value;
            }

            // If the target does not have an explicit style, try to find the default style
            {
                if (parameter is FrameworkElement fe)
                {
                    return fe.TryFindResource(fe.GetType()) as Style;
                }

                if (parameter is FrameworkContentElement fec)
                {
                    return fec.TryFindResource(fec.GetType()) as Style;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}