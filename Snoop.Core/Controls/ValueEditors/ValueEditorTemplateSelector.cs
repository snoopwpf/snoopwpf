// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors;

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Helpers;

public class ValueEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? StandardTemplate { get; set; }

    public DataTemplate? EnumTemplate { get; set; }

    public DataTemplate? BoolTemplate { get; set; }

    public DataTemplate? StringTemplate { get; set; }

    public DataTemplate? BrushTemplate { get; set; }

    public DataTemplate? ColorTemplate { get; set; }

    public DataTemplate? WithResourceKeyTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        var property = (PropertyInformation?)item;

        if (property is null)
        {
            return null;
        }

        if (property.PropertyType.IsEnum
            || Nullable.GetUnderlyingType(property.PropertyType.Type) is { IsEnum: true })
        {
            return this.EnumTemplate;
        }

        if (property.PropertyType.Type == typeof(bool)
            || property.PropertyType.Type == typeof(bool?))
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

        if (typeof(Color).IsAssignableFrom(property.PropertyType.Type)
            || typeof(Color?).IsAssignableFrom(property.PropertyType.Type))
        {
            return this.ColorTemplate;
        }

        if (ResourceKeyHelper.IsValidResourceKey(property.ResourceKey))
        {
            return this.WithResourceKeyTemplate;
        }

        return this.StandardTemplate;
    }
}