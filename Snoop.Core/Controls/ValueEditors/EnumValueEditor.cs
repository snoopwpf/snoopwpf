// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using JetBrains.Annotations;

public class EnumValueEditor : ValueEditor
{
    public ObservableCollection<EnumValueWrapper> Values { get; } = new();

    protected override void OnPropertyInfoChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyInfoChanged(e);

        this.Values.Clear();

        var enumType = this.PropertyInfo?.PropertyType.Type;

        if (enumType is not null)
        {
            if (enumType.IsEnum == false
                && Nullable.GetUnderlyingType(enumType) is { IsEnum: true } underlyingType)
            {
                enumType = underlyingType;

                this.Values.Add(new EnumValueWrapper(null, "<NULL>"));
            }

            var values = Enum.GetValues(enumType);
            var names = Enum.GetNames(enumType);

            for (var i = 0; i < values.Length; i++)
            {
                var value = values.GetValue(i);
                this.Values.Add(new EnumValueWrapper(value, names[i]));
            }
        }
    }

    protected override void OnValueChanged(object? newValue)
    {
        base.OnValueChanged(newValue);

        // sneaky trick here. only if both are non-null is this a change
        // caused by the user. If so, set the bool to track it.
        if (this.PropertyInfo is not null)
        {
            this.PropertyInfo.IsValueChangedByUser = true;
        }
    }
}

[PublicAPI]
public class EnumValueWrapper : INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public EnumValueWrapper(object? value, string text)
    {
        this.Value = value;
        this.Text = text;
    }

    public object? Value { get; }

    public string Text { get; }
}