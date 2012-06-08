// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
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
	}
}
