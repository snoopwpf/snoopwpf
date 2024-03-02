namespace Snoop.Converters;

using System;
using System.ComponentModel;
using Snoop.Infrastructure;

public static class StringValueConverter
{
    public static bool CanConvertFromString(Type? targetType)
    {
        if (targetType is null)
        {
            return false;
        }

        if (targetType.IsAssignableFrom(typeof(string)))
        {
            return true;
        }

        var converter = TypeDescriptor.GetConverter(targetType);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (converter is null)
        {
            return false;
        }

        if (converter.CanConvertFrom(typeof(string)) == false)
        {
            return false;
        }

        return true;
    }

    public static object? ConvertFromString(Type? targetType, string? value)
    {
        if (targetType is null)
        {
            return null;
        }

        if (targetType.IsAssignableFrom(typeof(string)))
        {
            return value;
        }

        var converter = TypeDescriptor.GetConverter(targetType);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (converter is null)
        {
            return null;
        }

        if (converter.CanConvertFrom(typeof(string)) == false)
        {
            return value;
        }

        // ReSharper restore HeuristicUnreachableCode
        // ReSharper restore ConditionIsAlwaysTrueOrFalse

        try
        {
            using (new InvariantThreadCultureScope())
            {
                return GetValueFromConverter(value, converter, true);
            }
        }
        catch
        {
            // If we land here the problem might have been related to the threads culture.
            // If the user entered data that was culture specific, we try setting it again using the original culture and not an invariant.
            try
            {
                return GetValueFromConverter(value, converter, false);
            }
            catch
            {
                // todo: How should we notify the user about failures?
            }
        }

        return null;
    }

    public static string? ConvertToString(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return ConvertToString(value.GetType(), value);
    }

    public static string? ConvertToString(Type sourceType, object? value)
    {
        if (typeof(string).IsAssignableFrom(sourceType))
        {
            return (string?)value;
        }

        var converter = TypeDescriptor.GetConverter(sourceType);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (converter is null)
        {
            return null;
        }

        if (converter.CanConvertTo(typeof(string)) == false)
        {
            return value?.ToString();
        }

        // ReSharper restore HeuristicUnreachableCode
        // ReSharper restore ConditionIsAlwaysTrueOrFalse

        try
        {
            using (new InvariantThreadCultureScope())
            {
                return GetStringValueFromConverter(value, converter, true);
            }
        }
        catch
        {
            // If we land here the problem might have been related to the threads culture.
            // If the user entered data that was culture specific, we try setting it again using the original culture and not an invariant.
            try
            {
                return GetStringValueFromConverter(value, converter, false);
            }
            catch
            {
                // todo: How should we notify the user about failures?
            }
        }

        return null;
    }

    private static object? GetValueFromConverter(string? value, TypeConverter converter, bool invariant)
    {
        if (invariant)
        {
            return converter.ConvertFromInvariantString(value!);
        }

        return converter.ConvertFromString(value!);
    }

    private static string? GetStringValueFromConverter(object? value, TypeConverter converter, bool invariant)
    {
        if (invariant)
        {
            return converter.ConvertToInvariantString(value);
        }

        return converter.ConvertToString(value);
    }
}