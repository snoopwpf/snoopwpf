// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

/// <summary>
/// Allows committing a value when pressing Return, and requerying the value after setting it.
/// </summary>
public class EditTextBox : TextBox
{
    static EditTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EditTextBox), new FrameworkPropertyMetadata(typeof(EditTextBox)));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Return)
        {
            var expression = BindingOperations.GetBindingExpressionBase(this, TextProperty);
            expression?.UpdateSource();

            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}