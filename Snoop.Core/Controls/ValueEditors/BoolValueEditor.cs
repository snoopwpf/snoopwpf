// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System.Windows;
using System.Windows.Controls;

public class BoolValueEditor : ValueEditor
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (this.Template.FindName("PART_CheckBox", this) is CheckBox checkBox)
        {
            checkBox.Click += this.CheckBoxClickedHandler;
        }
    }

    private void CheckBoxClickedHandler(object sender, RoutedEventArgs e)
    {
        if (this.PropertyInfo is not null)
        {
            this.PropertyInfo.IsValueChangedByUser = true;
        }
    }
}