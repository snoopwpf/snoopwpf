// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma warning disable CA1819

namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

public class PropertyFilter : INotifyPropertyChanged
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

    private MatcherBase? matcher;
    private string filterString = string.Empty;
    private bool useRegex;
    private string? error;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PropertyFilter(string filterString, bool showDefaults)
    {
        this.FilterString = filterString;
        this.ShowDefaults = showDefaults;
    }

    public string FilterString
    {
        get => this.filterString;
        set
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            this.filterString = value ?? string.Empty;

            this.UpdateMatcher();
        }
    }

    public bool UseRegex
    {
        get => this.useRegex;
        set
        {
            this.useRegex = value;

            this.UpdateMatcher();
        }
    }

    public bool ShowDefaults { get; set; }

    public bool ShowUncommonProperties { get; set; }

    public PropertyFilterSet? SelectedFilterSet { get; set; }

    public bool HasError => string.IsNullOrEmpty(this.Error) is false;

    public string? Error
    {
        get => this.error;
        set
        {
            this.error = value;
            this.OnPropertyChanged();
            this.OnPropertyChanged(nameof(this.HasError));
        }
    }

    public bool ShouldShow(PropertyInformation property)
    {
        if (this.ShowUncommonProperties == false
            && IsUncommonProperty(property))
        {
            return false;
        }

        if (this.matcher is not null)
        {
            return this.matcher.Matches(property);
        }

        // else use the filter set if we have one of those.
        if (this.SelectedFilterSet?.Properties is not null)
        {
            return this.SelectedFilterSet!.IsPropertyInFilter(property);
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

    private void UpdateMatcher()
    {
        this.Error = null;

        if (string.IsNullOrEmpty(this.FilterString))
        {
            this.matcher = null;
            return;
        }

        if (this.UseRegex)
        {
            try
            {
                this.matcher = new RegexMatcher(this.FilterString);
            }
            catch (Exception e)
            {
                this.Error = e.Message;
            }
        }
        else
        {
            this.matcher = new StringContainsMatcher(this.FilterString);
        }
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    private abstract class MatcherBase
    {
        protected MatcherBase(string filter)
        {
            this.Filter = filter;
        }

        public string Filter { get; }

        public bool Matches(PropertyInformation propertyInformation)
        {
            if (this.Matches(propertyInformation.DisplayName))
            {
                return true;
            }

            if (propertyInformation.Property is not null
                && this.Matches(propertyInformation.Property.PropertyType.Name))
            {
                return true;
            }

            if (this.Matches(propertyInformation.DescriptiveValue))
            {
                return true;
            }

            return false;
        }

        protected abstract bool Matches(string value);
    }

    private class StringContainsMatcher : MatcherBase
    {
        public StringContainsMatcher(string filter)
            : base(filter)
        {
        }

        protected override bool Matches(string value)
        {
            return value.ContainsIgnoreCase(this.Filter);
        }
    }

    private class RegexMatcher : MatcherBase
    {
        private readonly Regex regex;

        public RegexMatcher(string filter)
            : base(filter)
        {
            this.regex = new Regex(filter, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        }

        protected override bool Matches(string value)
        {
            return this.regex.IsMatch(value);
        }
    }
}