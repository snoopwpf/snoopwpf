// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma warning disable CA1819

namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

public class PropertyFilter
{
    private static readonly List<Type> uncommonTypes = new()
    {
        typeof(BaseUriHelper),
        typeof(Block),
        typeof(ContextMenuService),
        typeof(DesignerProperties),
        typeof(InputLanguageManager),
        typeof(InputMethod),
        typeof(NameScope),
        typeof(NavigationService),
        typeof(NumberSubstitution),
        typeof(SpellCheck),
        typeof(Stylus),
        typeof(TextSearch),
        typeof(Timeline),
        typeof(ToolBar),
        typeof(ToolTipService),
        typeof(Typography),
        typeof(VirtualizingPanel),
        typeof(VisualStateManager),
        typeof(XmlAttributeProperties)
    };

    private static readonly List<PropertyDescriptor> nonUncommonProperties = new()
    {
    };

    private static readonly List<DependencyProperty> nonUncommonDependencyProperties = new()
    {
        ContextMenuService.ContextMenuProperty,
        ToolTipService.ToolTipProperty
    };

    private static readonly List<string> uncommonPropertyNames = new()
    {
        "Binding.XmlNamespaceManager"
    };

    private Regex? filterRegex;
    private string? filterString;
    private bool hasFilterString;

    public PropertyFilter(string filterString, bool showDefaults)
    {
        this.FilterString = filterString;
        this.ShowDefaults = showDefaults;
    }

    public string? FilterString
    {
        get => this.filterString;
        set
        {
            this.filterString = value;
            this.hasFilterString = string.IsNullOrEmpty(this.filterString) == false;

            if (this.hasFilterString == false)
            {
                this.filterRegex = null;
                return;
            }

            try
            {
                this.filterRegex = new Regex(this.FilterString!, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            }
            catch
            {
                this.filterRegex = null;
            }
        }
    }

    public bool ShowDefaults { get; set; }

    public bool ShowUncommonProperties { get; set; }

    public PropertyFilterSet? SelectedFilterSet { get; set; }

    public bool IsPropertyFilterSet => this.SelectedFilterSet?.Properties is not null;

    public bool ShouldShow(PropertyInformation property)
    {
        if (this.ShowUncommonProperties == false
            && IsUncommonProperty(property))
        {
            return false;
        }

        // use a regular expression if we have one and we also have a filter string.
        if (this.hasFilterString
            && this.filterRegex is not null)
        {
            if (this.filterRegex.IsMatch(property.DisplayName))
            {
                return true;
            }

            if (property.Property is not null
                && this.filterRegex.IsMatch(property.Property.PropertyType.Name))
            {
                return true;
            }

            return false;
        }

        // else just check for containment if we don't have a regular expression but we do have a filter string.
        if (this.hasFilterString)
        {
            if (property.DisplayName.ContainsIgnoreCase(this.FilterString))
            {
                return true;
            }

            if (property.Property is not null
                && property.Property.PropertyType.Name.ContainsIgnoreCase(this.FilterString))
            {
                return true;
            }

            return false;
        }

        // else use the filter set if we have one of those.
        if (this.IsPropertyFilterSet)
        {
            if (this.SelectedFilterSet!.IsPropertyInFilter(property))
            {
                return true;
            }

            return false;
        }

        // finally, if none of the above applies
        // just check to see if we're not showing properties at their default values
        // and this property is actually set to its default value
        if (this.ShouldShowDefaultValueFilter(property) == false)
        {
            return false;
        }

        return true;
    }

    private bool ShouldShowDefaultValueFilter(PropertyInformation property)
    {
        if (this.ShowDefaults)
        {
            return true;
        }

        if (property.ValueSource.BaseValueSource != BaseValueSource.Default)
        {
            return true;
        }

        // Always show common properties
        if (property.DependencyProperty == ColumnDefinition.WidthProperty
            || property.DependencyProperty == RowDefinition.HeightProperty)
        {
            return true;
        }

        return false;
    }

    private static bool IsUncommonProperty(PropertyInformation property)
    {
        if (property.Property is null)
        {
            return false;
        }

        if (property.Property.IsBrowsable == false)
        {
            return false;
        }

        if (nonUncommonProperties.Contains(property.Property))
        {
            return false;
        }

        if (uncommonPropertyNames.Contains(property.Property.Name))
        {
            return true;
        }

        if (property.DependencyProperty is null)
        {
            return false;
        }

        if (nonUncommonDependencyProperties.Contains(property.DependencyProperty))
        {
            return false;
        }

        if (property.DependencyProperty.OwnerType.Namespace?.StartsWith("Snoop", StringComparison.Ordinal) == true)
        {
            return true;
        }

        return uncommonTypes.Contains(property.DependencyProperty.OwnerType);
    }
}