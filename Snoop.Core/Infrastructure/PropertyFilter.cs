// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.Serialization;
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
        private string? filterString;
        private bool hasFilterString;
        private Regex? filterRegex;

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
                    this.filterRegex = new Regex(this.filterString, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                }
                catch
                {
                    this.filterRegex = null;
                }
            }
        }

        public bool ShowDefaults { get; set; }

        public bool ShowPropertiesFromUncommonTypes { get; set; }

        public PropertyFilterSet? SelectedFilterSet { get; set; }

        public bool IsPropertyFilterSet => this.SelectedFilterSet?.Properties is not null;

        public bool Show(PropertyInformation property)
        {
            if (this.ShowPropertiesFromUncommonTypes == false
                && IsUncommonProperty(property))
            {
                return false;
            }

            // use a regular expression if we have one and we also have a filter string.
            if (this.hasFilterString
                && this.filterRegex is not null)
            {
                return this.filterRegex.IsMatch(property.DisplayName)
                       || (property.Property is not null && this.filterRegex.IsMatch(property.Property.PropertyType.Name));
            }

            // else just check for containment if we don't have a regular expression but we do have a filter string.
            else if (this.hasFilterString)
            {
                if (property.DisplayName.ContainsIgnoreCase(this.FilterString!))
                {
                    return true;
                }

                if (property.Property is not null
                    && property.Property.PropertyType.Name.ContainsIgnoreCase(this.FilterString!))
                {
                    return true;
                }

                return false;
            }

            // else use the filter set if we have one of those.
            else if (this.IsPropertyFilterSet)
            {
                if (this.SelectedFilterSet!.IsPropertyInFilter(property.DisplayName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // finally, if none of the above applies
            // just check to see if we're not showing properties at their default values
            // and this property is actually set to its default value
            else
            {
                if (this.ShowDefaults == false
                    && property.ValueSource.BaseValueSource == BaseValueSource.Default)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private static bool IsUncommonProperty(PropertyInformation property)
        {
            if (property.Property is null)
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

            return uncommonTypes.Contains(property.DependencyProperty.OwnerType);
        }

        private static readonly List<Type> uncommonTypes = new List<Type>
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
            typeof(XmlAttributeProperties),

            // Snoops own attached properties
            typeof(AttachedPropertyManager),
            typeof(BringIntoViewBehavior),
            typeof(ComboBoxSettings)
        };

        private static readonly List<string> uncommonPropertyNames = new List<string>
        {
            "Binding.XmlNamespaceManager"
        };
    }

    [DebuggerDisplay("{" + nameof(DisplayName) + "}")]
    [Serializable]
    public class PropertyFilterSet
    {
        public string? DisplayName { get; set; }

        public bool IsDefault { get; set; }

        public bool IsEditCommand { get; set; }

        [IgnoreDataMember]
        public bool IsReadOnly { get; set; }

        public string[]? Properties { get; set; }

        public bool IsPropertyInFilter(string property)
        {
            if (this.Properties is null)
            {
                return false;
            }

            foreach (var filterProp in this.Properties)
            {
                if (property.StartsWith(filterProp, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public PropertyFilterSet Clone()
        {
            var src = this;
            return new PropertyFilterSet
            {
                DisplayName = src.DisplayName,
                IsDefault = src.IsDefault,
                IsEditCommand = src.IsEditCommand,
                IsReadOnly = src.IsReadOnly,
                Properties = (string[]?)src.Properties?.Clone()
            };
        }
    }
}