// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Snoop.Controls.ValueEditors.Details;
using Snoop.Infrastructure;

public abstract class ValueEditor : ContentControl
{
    public static readonly RoutedCommand OpenDetailsEditorCommand = new(nameof(OpenDetailsEditorCommand), typeof(ValueEditor));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(ValueEditor));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(object),
            typeof(ValueEditor),
            new PropertyMetadata(OnValueChanged));

    public static readonly DependencyProperty DescriptiveValueProperty =
        DependencyProperty.Register(
            nameof(DescriptiveValue),
            typeof(string),
            typeof(ValueEditor));

    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(
            nameof(IsEditable),
            typeof(bool),
            typeof(ValueEditor));

    public static readonly DependencyProperty PropertyInfoProperty =
        DependencyProperty.Register(
            nameof(PropertyInfo),
            typeof(PropertyInformation),
            typeof(ValueEditor),
            new UIPropertyMetadata(null, OnPropertyInfoChanged));

    public static readonly DependencyProperty SupportsDetailsEditorProperty =
        DependencyProperty.Register(
            nameof(SupportsDetailsEditor),
            typeof(bool),
            typeof(ValueEditor),
            new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty DetailsEditorTemplateProperty =
        DependencyProperty.Register(
            nameof(DetailsEditorTemplate),
            typeof(DataTemplate),
            typeof(ValueEditor),
            new PropertyMetadata(default(DataTemplate)));

    public ValueEditor()
    {
        this.CommandBindings.Add(new CommandBinding(OpenDetailsEditorCommand, this.HandleOpenDetailsEditorCommand, this.HandleCanOpenDetailsEditorCommand));
    }

    public DataTemplate? DetailsEditorTemplate
    {
        get => (DataTemplate?)this.GetValue(DetailsEditorTemplateProperty);
        set => this.SetValue(DetailsEditorTemplateProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)this.GetValue(IsSelectedProperty);
        set => this.SetValue(IsSelectedProperty, value);
    }

    public object? Value
    {
        get => this.GetValue(ValueProperty);
        set => this.SetValue(ValueProperty, value);
    }

    public string? DescriptiveValue
    {
        get => (string?)this.GetValue(DescriptiveValueProperty);
        set => this.SetValue(DescriptiveValueProperty, value);
    }

    public bool IsEditable
    {
        get => (bool)this.GetValue(IsEditableProperty);
        set => this.SetValue(IsEditableProperty, value);
    }

    public PropertyInformation? PropertyInfo
    {
        get => (PropertyInformation?)this.GetValue(PropertyInfoProperty);
        set => this.SetValue(PropertyInfoProperty, value);
    }

    public bool SupportsDetailsEditor
    {
        get => (bool)this.GetValue(SupportsDetailsEditorProperty);
        set => this.SetValue(SupportsDetailsEditorProperty, value);
    }

    private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        ((ValueEditor)sender).OnValueChanged(e.NewValue);
    }

    protected virtual void OnValueChanged(object? newValue)
    {
    }

    private static void OnPropertyInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ValueEditor)d).OnPropertyInfoChanged(e);
    }

    protected virtual void OnPropertyInfoChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    private void HandleCanOpenDetailsEditorCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = this.SupportsDetailsEditor;
    }

    private void HandleOpenDetailsEditorCommand(object sender, ExecutedRoutedEventArgs e)
    {
        ValueEditorDetailsWindow.ShowDialog(this);
    }

    public virtual void PrepareForDetailsEditor()
    {
    }

    public virtual void AcceptValueFromDetailsEditor()
    {
        if (this.PropertyInfo is not null)
        {
            this.PropertyInfo.IsValueChangedByUser = true;
        }
    }
}