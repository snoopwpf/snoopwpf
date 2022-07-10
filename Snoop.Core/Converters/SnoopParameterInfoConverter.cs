// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Converters;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Snoop.Views.MethodsTab;

[ValueConversion(typeof(SnoopParameterInformation), typeof(object))]
public class SnoopParameterInfoConverter : IValueConverter
{
    public static readonly SnoopParameterInfoConverter Default = new();

    #region IValueConverter Members

    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SnoopParameterInformation paramInfo)
        {
            return value;
        }

        if (paramInfo.ParameterValue is null)
        {
            return null;
        }

        var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);

        var result = converter.ConvertFrom(paramInfo.ParameterValue);

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    #endregion
}

public class SnoopDependencyPropertiesConverter : IValueConverter
{
    public static readonly SnoopDependencyPropertiesConverter Default = new();

    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var paramInfo = (SnoopParameterInformation)value;
        var t = paramInfo.DeclaringType;

        var fields = t.Type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static);

        var dpType = typeof(DependencyProperty);

        var dependencyProperties = new List<DependencyPropertyNameValuePair>();

        foreach (var field in fields)
        {
            if (dpType.IsAssignableFrom(field.FieldType))
            {
                dependencyProperties.Add(new DependencyPropertyNameValuePair(field.Name, (DependencyProperty?)field.GetValue(null)));
            }
        }

        dependencyProperties.Sort();

        return dependencyProperties;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    #endregion
}

public class DependencyPropertyNameValuePair : IComparable
{
    public DependencyPropertyNameValuePair(string dependencyPropertyName, DependencyProperty? dependencyProperty)
    {
        this.DependencyPropertyName = dependencyPropertyName;
        this.DependencyProperty = dependencyProperty;
    }

    public string DependencyPropertyName { get; }

    public DependencyProperty? DependencyProperty { get; }

    public override string ToString()
    {
        return this.DependencyPropertyName;
    }

    #region IComparable Members

    public int CompareTo(object? obj)
    {
        var toCompareTo = (DependencyPropertyNameValuePair?)obj;

        return string.Compare(this.DependencyPropertyName, toCompareTo?.DependencyPropertyName, StringComparison.Ordinal);
    }

    #endregion
}

[ValueConversion(typeof(object), typeof(object))]
[ValueConversion(typeof(Enum), typeof(Array))]
[ValueConversion(typeof(bool), typeof(object[]))]
public class SnoopEnumValuesConverter : IValueConverter
{
    public static readonly SnoopEnumValuesConverter Default = new();

    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is Enum)
        {
            return Enum.GetValues(value.GetType());
        }

        if (value is bool)
        {
            return new object[] { true, false };
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}