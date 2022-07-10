// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Converters;

using System;
using System.Windows.Data;

/// <summary>
/// Converter to tell us if the given value is supported for MouseWheel editing.
/// Used to use app.config to drive this, but now it's just a hardcoded list
/// </summary>
public class IsSupportedRuntimeTypeConverter : IValueConverter
{
    public static readonly IsSupportedRuntimeTypeConverter Default = new();

    private static readonly string[] ExcludeTypeNames = { "LinearGradientBrush", "RadialGradientBrush", "TileBrush" };

    #region IValueConverter Members
    public object Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // value (object)   the runtime value - compare against known incompatible types (use list from config)
        // return (boolean) whether the type is supported or not

        if (value is null)
        {
            return false;
        }

        foreach (var excludeTypeName in ExcludeTypeNames)
        {
            if (value.GetType().Name == excludeTypeName)
            {
                return false;
            }
        }

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    #endregion
}