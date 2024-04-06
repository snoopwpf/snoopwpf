// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System.Windows;
using System.Windows.Data;
using Snoop.Converters;

public class StandardValueEditor : ValueEditor
{
    public static readonly DependencyProperty StringValueProperty =
        DependencyProperty.Register(
            nameof(StringValue),
            typeof(string),
            typeof(StandardValueEditor),
            new PropertyMetadata(OnStringValueChanged));

    private bool isUpdatingValue;

    public string? StringValue
    {
        get => (string?)this.GetValue(StringValueProperty);
        set => this.SetValue(StringValueProperty, value);
    }

    private static void OnStringValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        ((StandardValueEditor)sender).OnStringValueChanged((string?)e.NewValue);
    }

    protected virtual void OnStringValueChanged(string? newValue)
    {
        if (this.isUpdatingValue)
        {
            return;
        }

        if (this.PropertyInfo is not null)
        {
            this.PropertyInfo.IsValueChangedByUser = true;
        }

        var targetType = this.PropertyInfo?.PropertyType.Type;

        this.Value = StringValueConverter.ConvertFromString(targetType, newValue);
    }

    protected override void OnValueChanged(object? newValue)
    {
        this.isUpdatingValue = true;

        var value = this.Value;

        this.StringValue = StringValueConverter.ConvertToString(value) ?? string.Empty;

        this.isUpdatingValue = false;

        var binding = BindingOperations.GetBindingExpression(this, StringValueProperty);
        binding?.UpdateSource();
    }

    protected override void OnPropertyInfoChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyInfoChanged(e);

        this.IsEditable = this.PropertyInfo?.CanEdit is true
                          && StringValueConverter.CanConvertFromString(this.PropertyInfo?.PropertyType.Type);
    }
}