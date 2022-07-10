namespace Snoop.Controls.ValueEditors.Details;

using System.Windows;

public partial class ValueEditorDetailsWindow
{
    public static readonly DependencyProperty ValueEditorProperty = DependencyProperty.Register(nameof(ValueEditor), typeof(ValueEditor), typeof(ValueEditorDetailsWindow), new PropertyMetadata(default(ValueEditor)));

    public ValueEditorDetailsWindow()
    {
        this.InitializeComponent();
    }

    public ValueEditor? ValueEditor
    {
        get => (ValueEditor?)this.GetValue(ValueEditorProperty);
        set => this.SetValue(ValueEditorProperty, value);
    }

    public static void ShowDialog(ValueEditor valueEditor)
    {
        var window = new ValueEditorDetailsWindow
        {
            ValueEditor = valueEditor
        };

        valueEditor.PrepareForDetailsEditor();

        if (window.ShowDialogEx(valueEditor) == true)
        {
            valueEditor.AcceptValueFromDetailsEditor();
        }
    }

    private void ChangeValue_OnClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;

        this.Close();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}