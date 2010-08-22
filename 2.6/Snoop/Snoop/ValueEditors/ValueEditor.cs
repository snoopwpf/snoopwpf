// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	public class ValueEditor: ContentControl {
		public static DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(ValueEditor));
		public static DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(ValueEditor), new PropertyMetadata(ValueEditor.HandleValueChanged));
		public static DependencyProperty PropertyTypeProperty = DependencyProperty.Register("PropertyType", typeof(object), typeof(ValueEditor), new PropertyMetadata(ValueEditor.HandleTypeChanged));
		public static DependencyProperty IsEditableProperty = DependencyProperty.Register("IsEditable", typeof(bool), typeof(ValueEditor));

		public bool IsSelected {
			get { return (bool)this.GetValue(ValueEditor.IsSelectedProperty); }
			set { this.SetValue(ValueEditor.IsSelectedProperty, value); }
		}

		public object Value {
			get { return this.GetValue(ValueEditor.ValueProperty); }
			set { this.SetValue(ValueEditor.ValueProperty, value); }
		}

		public Type PropertyType {
			get { return (Type)this.GetValue(ValueEditor.PropertyTypeProperty); }
			set { this.SetValue(ValueEditor.PropertyTypeProperty, value); }
		}

		public bool IsEditable {
			get { return (bool)this.GetValue(ValueEditor.IsEditableProperty); }
			set { this.SetValue(ValueEditor.IsEditableProperty, value); }
		}

		private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
			((ValueEditor)sender).OnValueChanged(e.NewValue);
		}

		protected virtual void OnValueChanged(object newValue) {
		}

		private static void HandleTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
			((ValueEditor)sender).OnTypeChanged();
		}

		protected virtual void OnTypeChanged() {
		}
	}
}
