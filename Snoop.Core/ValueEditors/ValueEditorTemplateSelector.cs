// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class ValueEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StandardTemplate { get; set; }

        public DataTemplate EnumTemplate { get; set; }

        public DataTemplate BoolTemplate { get; set; }

        public DataTemplate BrushTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var property = (PropertyInformation)item;

            if (property == null)
            {
                return null;
            }

            if (property.PropertyType.IsEnum)
            {
                return this.EnumTemplate;
            }

            if (property.PropertyType == typeof(bool))
            {
                return this.BoolTemplate;
            }

            if (property.PropertyType.IsGenericType
                && Nullable.GetUnderlyingType(property.PropertyType) == typeof(bool))
            {
                return this.BoolTemplate;
            }

            if (typeof(Brush).IsAssignableFrom(property.PropertyType))
            {
                return this.BrushTemplate;
            }

            return this.StandardTemplate;
        }
    }
}