// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Snoop.ValueEditors.Details;

    public abstract class ValueEditor : ContentControl
    {
        public static readonly RoutedCommand OpenDetailsEditorCommand = new RoutedCommand();

        public static DependencyProperty IsSelectedProperty =
            DependencyProperty.Register
            (
                nameof(IsSelected),
                typeof(bool),
                typeof(ValueEditor)
            );

        public static DependencyProperty ValueProperty =
            DependencyProperty.Register
            (
                nameof(Value),
                typeof(object),
                typeof(ValueEditor),
                new PropertyMetadata(HandleValueChanged)
            );

        public static DependencyProperty DescriptiveValueProperty =
            DependencyProperty.Register
            (
                nameof(DescriptiveValue),
                typeof(object),
                typeof(ValueEditor)
            );

        public static DependencyProperty PropertyTypeProperty =
            DependencyProperty.Register
            (
                nameof(PropertyType),
                typeof(object),
                typeof(ValueEditor),
                new PropertyMetadata(HandleTypeChanged)
            );

        public static DependencyProperty IsEditableProperty =
            DependencyProperty.Register
            (
                nameof(IsEditable),
                typeof(bool),
                typeof(ValueEditor)
            );

        public static readonly DependencyProperty PropertyInfoProperty =
            DependencyProperty.Register
            (
                nameof(PropertyInfo),
                typeof(PropertyInformation),
                typeof(ValueEditor),
                new UIPropertyMetadata(null, OnPropertyInfoChanged)
            );

        public static readonly DependencyProperty SupportsDetailsEditorProperty =
            DependencyProperty.Register
            (
                nameof(SupportsDetailsEditor),
                typeof(bool),
                typeof(ValueEditor),
                new PropertyMetadata(default(bool))
            );

        public static readonly DependencyProperty DetailsEditorTemplateProperty = 
            DependencyProperty.Register
            (
                nameof(DetailsEditorTemplate), 
                typeof(DataTemplate), 
                typeof(ValueEditor), 
                new PropertyMetadata(default(DataTemplate))
            );

        public ValueEditor()
        {
            this.CommandBindings.Add(new CommandBinding(OpenDetailsEditorCommand, this.HandleOpenDetailsEdtiorCommand, this.HandleCanOpenDetailsEditorCommand));
        }

        public DataTemplate DetailsEditorTemplate
        {
            get => (DataTemplate)this.GetValue(DetailsEditorTemplateProperty);
            set => this.SetValue(DetailsEditorTemplateProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)this.GetValue(IsSelectedProperty);
            set => this.SetValue(IsSelectedProperty, value);
        }

        public object Value
        {
            get => this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        public object DescriptiveValue
        {
            get => (bool)this.GetValue(DescriptiveValueProperty);
            set => this.SetValue(DescriptiveValueProperty, value);
        }

        public Type PropertyType
        {
            get => (Type)this.GetValue(PropertyTypeProperty);
            set => this.SetValue(PropertyTypeProperty, value);
        }

        public bool IsEditable
        {
            get => (bool)this.GetValue(IsEditableProperty);
            set => this.SetValue(IsEditableProperty, value);
        }

        public PropertyInformation PropertyInfo
        {
            get => (PropertyInformation)this.GetValue(PropertyInfoProperty);
            set => this.SetValue(PropertyInfoProperty, value);
        }

        public bool SupportsDetailsEditor
        {
            get => (bool)this.GetValue(SupportsDetailsEditorProperty);
            set => this.SetValue(SupportsDetailsEditorProperty, value);
        }

        private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ValueEditor)sender).OnValueChanged(e.NewValue);
        }

        protected virtual void OnValueChanged(object newValue)
        {
        }

        private static void HandleTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ValueEditor)sender).OnTypeChanged();
        }

        protected virtual void OnTypeChanged()
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

        private void HandleOpenDetailsEdtiorCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ValueEditorDetailsWindow.ShowDialog(this);
        }

        public virtual void PrepareForDetailsEditor()
        {
        }

        public virtual void AcceptValueFromDetailsEditor()
        {
            if (this.PropertyInfo != null)
            {
                this.PropertyInfo.IsValueChangedByUser = true;
            }
        }
    }
}