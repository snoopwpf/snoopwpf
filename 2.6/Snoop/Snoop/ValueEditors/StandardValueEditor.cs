// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
	using System.Windows;
	using System;
	using System.ComponentModel;
	using System.Windows.Data;

	public partial class StandardValueEditor: ValueEditor
	{
		public static readonly DependencyProperty StringValueProperty = DependencyProperty.Register("StringValue", typeof(string), typeof(StandardValueEditor), new PropertyMetadata(StandardValueEditor.HandleStringPropertyChanged));
		private bool isUpdatingValue = false;

		public StandardValueEditor() {
		}

		public string StringValue {
			get { return (string)this.GetValue(StandardValueEditor.StringValueProperty); }
			set { this.SetValue(StandardValueEditor.StringValueProperty, value); }
		}

		private static void HandleStringPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
			((StandardValueEditor)sender).OnStringPropertyChanged((string)e.NewValue);
		}

		protected virtual void OnStringPropertyChanged(string newValue) {
			if (this.isUpdatingValue)
				return;

			Type targetType = this.PropertyType;

			if (targetType.IsAssignableFrom(typeof(string)))
				this.Value = newValue;
			else {
				TypeConverter converter = TypeDescriptor.GetConverter(targetType);
				if (converter != null) {
					try {
						this.Value = converter.ConvertFrom(newValue);
					}
					catch (Exception) { }
				}
			}
		}

		protected override void OnValueChanged(object newValue) {

			this.isUpdatingValue = true;

			object value = this.Value;
			if (value != null)
				this.StringValue = value.ToString();
			else
				this.StringValue = string.Empty;

			this.isUpdatingValue = false;

			BindingExpression binding = BindingOperations.GetBindingExpression(this, StandardValueEditor.StringValueProperty);
			if (binding != null)
				binding.UpdateSource();
		}
	}
}
