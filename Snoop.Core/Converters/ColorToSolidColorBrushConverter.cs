namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

[ValueConversion(typeof(Color), typeof(SolidColorBrush))]
public sealed class ColorToSolidColorBrushConverter : IValueConverter
{
  public static ColorToSolidColorBrushConverter DefaultInstance { get; } = new();

  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
        {
            return DependencyProperty.UnsetValue;
        }

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush)
        {
            return DependencyProperty.UnsetValue;
        }

        return brush.Color;
    }
}