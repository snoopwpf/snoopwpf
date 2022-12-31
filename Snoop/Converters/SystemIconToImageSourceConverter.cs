namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Snoop.Infrastructure;

[ValueConversion(typeof(SystemIcon), typeof(ImageSource))]
public class SystemIconToImageSourceConverter : IValueConverter
{
    public static readonly SystemIconToImageSourceConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var systemIcon = (SystemIcon)value;
        var size = System.Convert.ToInt32(parameter);

        return SystemIconHelper.GetImageSource(systemIcon, size, size);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}