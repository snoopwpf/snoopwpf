// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System.Windows;

public class StringValueEditor : StandardValueEditor
{
    public static readonly DependencyProperty StringValueForDetailsEditorProperty = DependencyProperty.Register(
        nameof(StringValueForDetailsEditor), typeof(string), typeof(StringValueEditor), new PropertyMetadata(default(string)));

    public StringValueEditor()
    {
        this.SupportsDetailsEditor = true;
    }

    public string? StringValueForDetailsEditor
    {
        get => (string?)this.GetValue(StringValueForDetailsEditorProperty);
        set => this.SetValue(StringValueForDetailsEditorProperty, value);
    }

    public override void PrepareForDetailsEditor()
    {
        base.PrepareForDetailsEditor();

        this.StringValueForDetailsEditor = this.StringValue;
    }

    public override void AcceptValueFromDetailsEditor()
    {
        base.AcceptValueFromDetailsEditor();

        this.StringValue = this.StringValueForDetailsEditor;
    }
}