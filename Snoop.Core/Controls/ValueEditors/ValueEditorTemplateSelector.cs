// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Snoop.Infrastructure;

    public class ValueEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? StandardTemplate { get; set; }

        public DataTemplate? EnumTemplate { get; set; }

        public DataTemplate? BoolTemplate { get; set; }

        public DataTemplate? StringTemplate { get; set; }

        public DataTemplate? BrushTemplate { get; set; }

        public DataTemplate? WithResourceKeyTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            var property = (PropertyInformation?)item;

            if (property is null)
            {
                return null;
            }

            if (property.PropertyType.IsEnum)
            {
                return this.EnumTemplate;
            }

            if (property.PropertyType.Type == typeof(bool)
                || (property.PropertyType.IsGenericType
                    && Nullable.GetUnderlyingType(property.PropertyType.Type) == typeof(bool)))
            {
                return this.BoolTemplate;
            }

            if (property.PropertyType.Type == typeof(string))
            {
                return this.StringTemplate;
            }

            if (typeof(Brush).IsAssignableFrom(property.PropertyType.Type))
            {
                return this.BrushTemplate;
            }

            if (string.IsNullOrEmpty(property.ResourceKey) == false)
            {
                return this.WithResourceKeyTemplate;
            }

            return this.StandardTemplate;
        }
    }
}