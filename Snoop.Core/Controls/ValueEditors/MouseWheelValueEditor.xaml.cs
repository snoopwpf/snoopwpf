// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Snoop.Infrastructure;

/// <summary>
///     Interaction logic for MouseWheelValueEditor.xaml
/// </summary>
public partial class MouseWheelValueEditor
{
    public MouseWheelValueEditor()
    {
        this.InitializeComponent();

        this.MouseWheel += this.MouseWheelHandler;
    }

    private PropertyInformation? PropertyInfo => this.DataContext as PropertyInformation;

    private void MouseWheelHandler(object sender, MouseWheelEventArgs e)
    {
        var fe = e.OriginalSource as FrameworkElement;
        if (fe is null)
        {
            return;
        }

        var increment = true;
        var largeIncrement = false;
        var tinyIncrement = false;

        if (e.Delta > 0)
        {
            increment = false;
        }

        if ((Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) > 0
            || (Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) > 0)
        {
            largeIncrement = true;
        }

        if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) > 0
            || (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) > 0)
        {
            tinyIncrement = true;
        }

        var tb = fe as TextBlock;
        if (tb is not null
            && tb.Tag is not null
            && this.PropertyInfo?.Property is not null)
        {
            var fieldNum = int.Parse(tb.Tag.ToString()!);

            switch (this.PropertyInfo.PropertyType.Name)
            {
                case "Double":
                    this.PropertyInfo.StringValue = ChangeDoubleValue(tb.Text, increment, largeIncrement, tinyIncrement);
                    break;

                case "Int32":
                case "Int16":
                    this.PropertyInfo.StringValue = ChangeIntValue(tb.Text, increment, largeIncrement);
                    break;

                case "Boolean":
                    this.PropertyInfo.StringValue = ChangeBooleanValue(tb.Text);
                    break;

                case "Visibility":
                    this.PropertyInfo.StringValue = ChangeEnumValue<Visibility>(tb.Text, increment);
                    break;

                case "HorizontalAlignment":
                    this.PropertyInfo.StringValue = ChangeEnumValue<HorizontalAlignment>(tb.Text, increment);
                    break;

                case "VerticalAlignment":
                    this.PropertyInfo.StringValue = ChangeEnumValue<VerticalAlignment>(tb.Text, increment);
                    break;

                case "Thickness":
                    this.ChangeThicknessValuePart(fieldNum, tb.Text, increment, largeIncrement);
                    break;

                case "Brush":
                    this.ChangeBrushValuePart(fieldNum, tb.Text, increment, largeIncrement);
                    break;

                case "Color":
                    this.ChangeBrushValuePart(fieldNum, tb.Text, increment, largeIncrement);
                    break;
            }

            this.PropertyInfo.IsValueChangedByUser = true;
        }

        e.Handled = true;
    }

    private static string ChangeIntValue(string current, bool increase, bool largeIncrement)
    {
        var change = 1;
        if (!increase)
        {
            change *= -1;
        }

        if (largeIncrement)
        {
            change *= 10;
        }

        var ret = int.Parse(current);
        ret += change;

        return ret.ToString();
    }

    private static string ChangeDoubleValue(string current, bool increase, bool largeIncrement, bool tinyIncrement)
    {
        double change = 1;
        if (!increase)
        {
            change *= -1;
        }

        if (largeIncrement)
        {
            change *= 10;
        }

        if (tinyIncrement)
        {
            change /= 10;
        }

        var ret = double.Parse(current);
        ret += change;

        return ret.ToString();
    }

    private static string ChangeBooleanValue(string current)
    {
        var ret = bool.Parse(current);
        ret = !ret;

        return ret.ToString();
    }

    private static string ChangeEnumValue<T>(string current, bool increase)
    {
        var ret = (T)Enum.Parse(typeof(T), current);

        // make numeric, so we can add or subtract one
        var value = Convert.ToInt32(ret);
        if (increase)
        {
            value += 1;
        }
        else
        {
            value -= 1;
        }

        value = Math.Min(Enum.GetValues(typeof(T)).Length - 1, Math.Max(0, value));
        // long way around to get the enum typed value from the integer
        ret = (T)Enum.GetValues(typeof(T)).GetValue(value)!;

        return ret.ToString()!;
    }

    /// <summary>
    ///     Increments or decrements the field part in the Thickness value.
    ///     Replaces that field in the underlying VALUE
    /// </summary>
    private void ChangeThicknessValuePart(int fieldNum, string current, bool increase, bool largeIncrement)
    {
        var change = 1;
        if (!increase)
        {
            change *= -1;
        }

        if (largeIncrement)
        {
            change *= 20;
        }

        var newVal = int.Parse(current);
        newVal += change;

        var partValue = newVal.ToString();
        var currentValue = this.PropertyInfo!.StringValue;

        // chop the current value up into its parts
        var fields = currentValue.Split(',');

        // replace the appropriate field
        fields[fieldNum - 1] = partValue;

        // re-assemble back to Brush value
        var newValue = string.Format(@"{0},{1},{2},{3}", fields[0], fields[1], fields[2], fields[3]);

        this.PropertyInfo.StringValue = newValue;
    }

    /// <summary>
    ///     Increments or decrements the field part in the Brush value.
    ///     Replaces that field in the underlying VALUE
    /// </summary>
    private void ChangeBrushValuePart(int fieldNum, string current, bool increase, bool largeIncrement)
    {
        var change = 1;
        if (!increase)
        {
            change *= -1;
        }

        if (largeIncrement)
        {
            change *= 16;
        }

        var ret = int.Parse(current, NumberStyles.HexNumber);
        ret = Math.Min(255, Math.Max(0, ret + change));

        var partValue = ret.ToString("X2");
        var currentValue = this.PropertyInfo!.StringValue;

        // chop the current value up into its parts
        var fields = new string[4];
        fields[0] = currentValue.Substring(1, 2); // start at 1 to skip the leading # sign
        fields[1] = currentValue.Substring(3, 2);
        fields[2] = currentValue.Substring(5, 2);
        fields[3] = currentValue.Substring(7, 2);

        // replace the appropriate field
        fields[fieldNum - 1] = partValue;

        // re-assemble back to Brush value
        var newValue = string.Format(@"#{0}{1}{2}{3}", fields[0], fields[1], fields[2], fields[3]);

        this.PropertyInfo.StringValue = newValue;
    }
}