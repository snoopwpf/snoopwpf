// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Snoop
{
	public class ValueEditor : ContentControl
	{
		public bool IsSelected
		{
			get { return (bool)this.GetValue(ValueEditor.IsSelectedProperty); }
			set { this.SetValue(ValueEditor.IsSelectedProperty, value); }
		}
		public static DependencyProperty IsSelectedProperty =
			DependencyProperty.Register
			(
				"IsSelected",
				typeof(bool),
				typeof(ValueEditor)
			);

		public object Value
		{
			get { return this.GetValue(ValueEditor.ValueProperty); }
			set { this.SetValue(ValueEditor.ValueProperty, value); }
		}
		public static DependencyProperty ValueProperty =
			DependencyProperty.Register
			(
				"Value",
				typeof(object),
				typeof(ValueEditor),
				new PropertyMetadata(ValueEditor.HandleValueChanged)
			);
		private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((ValueEditor)sender).OnValueChanged(e.NewValue);
		}
		protected virtual void OnValueChanged(object newValue)
		{
		}

		public object DescriptiveValue
		{
			get { return (bool)this.GetValue(ValueEditor.DescriptiveValueProperty); }
			set { this.SetValue(ValueEditor.DescriptiveValueProperty, value); }
		}
		public static DependencyProperty DescriptiveValueProperty =
			DependencyProperty.Register
			(
				"DescriptiveValue",
				typeof(object),
				typeof(ValueEditor)
			);

		public Type PropertyType
		{
			get { return (Type)this.GetValue(ValueEditor.PropertyTypeProperty); }
			set { this.SetValue(ValueEditor.PropertyTypeProperty, value); }
		}
		public static DependencyProperty PropertyTypeProperty =
			DependencyProperty.Register
			(
				"PropertyType",
				typeof(object),
				typeof(ValueEditor),
				new PropertyMetadata(ValueEditor.HandleTypeChanged)
			);
		private static void HandleTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((ValueEditor)sender).OnTypeChanged();
		}
		protected virtual void OnTypeChanged()
		{
		}

		public bool IsEditable
		{
			get { return (bool)this.GetValue(ValueEditor.IsEditableProperty); }
			set { this.SetValue(ValueEditor.IsEditableProperty, value); }
		}
		public static DependencyProperty IsEditableProperty =
			DependencyProperty.Register
			(
				"IsEditable",
				typeof(bool),
				typeof(ValueEditor)
			);

		public PropertyInformation PropertyInfo
		{
			[DebuggerStepThrough]
			get { return (PropertyInformation)GetValue(PropertyInfoProperty); }
			set { SetValue(PropertyInfoProperty, value); }
		}
		public static readonly DependencyProperty PropertyInfoProperty =
			DependencyProperty.Register
			(
				"PropertyInfo",
				typeof(PropertyInformation),
				typeof(ValueEditor),
				new UIPropertyMetadata(null, new PropertyChangedCallback(OnPropertyInfoChanged))
			);
		private static void OnPropertyInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((ValueEditor)d).OnPropertyInfoChanged(e);
		}
		private void OnPropertyInfoChanged(DependencyPropertyChangedEventArgs e)
		{
		}
	}
}
