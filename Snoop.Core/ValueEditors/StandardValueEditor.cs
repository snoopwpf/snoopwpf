// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    using Snoop.Infrastructure;

    public class StandardValueEditor : ValueEditor
    {
        public static readonly DependencyProperty StringValueProperty =
            DependencyProperty.Register
            (
                nameof(StringValue),
                typeof(string),
                typeof(StandardValueEditor),
                new PropertyMetadata(HandleStringPropertyChanged)
            );

        private bool isUpdatingValue;

        public string StringValue
        {
            get => (string)this.GetValue(StringValueProperty);
            set => this.SetValue(StringValueProperty, value);
        }

        private static void HandleStringPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((StandardValueEditor)sender).OnStringPropertyChanged((string)e.NewValue);
        }

        protected virtual void OnStringPropertyChanged(string newValue)
        {
            if (this.isUpdatingValue)
            {
                return;
            }

            if (this.PropertyInfo != null)
            {
                this.PropertyInfo.IsValueChangedByUser = true;
            }

            var targetType = this.PropertyType;

            if (targetType.IsAssignableFrom(typeof(string)))
            {
                this.Value = newValue;
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter != null)
                {
                    try
                    {
                        using (new InvariantThreadCultureScope())
                        {
                            this.SetValueFromConverter(newValue, targetType, converter);
                        }
                    }
                    catch
                    {
                        // If we land here the problem might have been related to the threads culture.
                        // If the user entered data that was culture specific, we try setting it again using the original culture and not an invariant.
                        try
                        {
                            this.SetValueFromConverter(newValue, targetType, converter);
                        }
                        catch
                        {
                            // todo: How should we notify the user about failures?
                        }
                    }
                }
            }
        }

        private void SetValueFromConverter(string newValue, Type targetType, TypeConverter converter)
        {
            if (!converter.CanConvertFrom(targetType) && string.IsNullOrEmpty(newValue))
            {
                this.Value = null;
            }
            else
            {
                this.Value = converter.ConvertFrom(newValue);
            }
        }

        protected override void OnValueChanged(object newValue)
        {
            this.isUpdatingValue = true;

            var value = this.Value;
            if (value != null)
            {
                using (new InvariantThreadCultureScope())
                {
                    this.StringValue = value.ToString();
                }
            }
            else
            {
                this.StringValue = string.Empty;
            }

            this.isUpdatingValue = false;

            var binding = BindingOperations.GetBindingExpression(this, StringValueProperty);
            binding?.UpdateSource();
        }
    }
}