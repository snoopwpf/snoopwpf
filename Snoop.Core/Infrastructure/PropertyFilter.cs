// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma warning disable CA1819

namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
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
    using JetBrains.Annotations;
    using Snoop.AttachedProperties;

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
            //typeof(ToolTipService),
            typeof(Typography),
            typeof(VirtualizingPanel),
            typeof(VisualStateManager),
            typeof(XmlAttributeProperties),

            // Snoops own attached properties
            typeof(AttachedPropertyManager),
            typeof(BringIntoViewBehavior),
            typeof(ComboBoxSettings),
            typeof(SnoopAttachedProperties)
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

        public bool Show(PropertyInformation property)
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
                return this.filterRegex.IsMatch(property.DisplayName)
                       || (property.Property is not null && this.filterRegex.IsMatch(property.Property.PropertyType.Name));
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

            if (this.ShowDefaults == false
                && property.ValueSource.BaseValueSource == BaseValueSource.Default)
            {
                return false;
            }

            return true;
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
    }

    [DebuggerDisplay("{" + nameof(DisplayName) + "}")]
    [Serializable]
    public class PropertyFilterSet : INotifyPropertyChanged
    {
        private string? displayName;
        private bool isDefault;
        private bool isEditCommand;
        private bool isReadOnly;
        private string[]? properties;

        public string? DisplayName
        {
            get => this.displayName;
            set
            {
                if (value == this.displayName)
                {
                    return;
                }

                this.displayName = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsDefault
        {
            get => this.isDefault;
            set
            {
                if (value == this.isDefault)
                {
                    return;
                }

                this.isDefault = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsEditCommand
        {
            get => this.isEditCommand;
            set
            {
                if (value == this.isEditCommand)
                {
                    return;
                }

                this.isEditCommand = value;
                this.OnPropertyChanged();
            }
        }

        [IgnoreDataMember]
        public bool IsReadOnly
        {
            get => this.isReadOnly;
            set
            {
                if (value == this.isReadOnly)
                {
                    return;
                }

                this.isReadOnly = value;
                this.OnPropertyChanged();
            }
        }

        public string[]? Properties
        {
            get => this.properties;
            set
            {
                if (Equals(value, this.properties))
                {
                    return;
                }

                this.properties = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsPropertyInFilter(PropertyInformation property)
        {
            if (this.Properties is null)
            {
                return false;
            }

            foreach (var filterProp in this.Properties)
            {
                if (property.Name?.Equals(filterProp, StringComparison.OrdinalIgnoreCase) == true
                    || property.DisplayName.StartsWith(filterProp, StringComparison.OrdinalIgnoreCase))
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}