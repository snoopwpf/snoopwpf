namespace Snoop.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    [ValueConversion(typeof(object), typeof(object))]
    [ValueConversion(typeof(object), typeof(Style))]
    public class NullStyleConverter : IValueConverter
    {
        public static readonly NullStyleConverter DefaultInstance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            if (value is not null)
            {
                return value;
            }

            // If the target does not have an explicit style, try to find the default style
            {
                if (parameter is FrameworkElement fe)
                {
                    var defaultStyleKey = FrameworkElementDefaultStyleKeyHelper.GetDefaultStyleKey(fe) ?? fe.GetType();
                    return fe.TryFindResource(defaultStyleKey) as Style;
                }

                if (parameter is FrameworkContentElement fce)
                {
                    var defaultStyleKey = FrameworkContentElementDefaultStyleKeyHelper.GetDefaultStyleKey(fce) ?? fce.GetType();
                    return fce.TryFindResource(defaultStyleKey) as Style;
                }
            }

            return null;
        }

        public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return Binding.DoNothing;
        }

#pragma warning disable CA1812

        private class FrameworkElementDefaultStyleKeyHelper : FrameworkElement
        {
            public static object? GetDefaultStyleKey(FrameworkElement element) => element.GetValue(DefaultStyleKeyProperty);
        }

        private class FrameworkContentElementDefaultStyleKeyHelper : FrameworkElement
        {
            public static object? GetDefaultStyleKey(FrameworkContentElement element) => element.GetValue(DefaultStyleKeyProperty);
        }

#pragma warning restore CA1812
    }
}