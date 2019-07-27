// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Text.RegularExpressions;

namespace Snoop
{
	public class PropertyFilter
	{
		private string filterString;
		private Regex filterRegex;
		private bool showDefaults;

		public PropertyFilter(string filterString, bool showDefaults)
		{
			this.filterString = filterString.ToLower();
			this.showDefaults = showDefaults;
		}

		public string FilterString
		{
			get { return this.filterString; }
			set
			{
				this.filterString = value.ToLower();
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

		public bool ShowDefaults
		{
			get { return this.showDefaults; }
			set { this.showDefaults = value; }
		}

		public PropertyFilterSet SelectedFilterSet { get; set; }

		public bool IsPropertyFilterSet
		{
			get
			{
				return (SelectedFilterSet != null && SelectedFilterSet.Properties != null);
			}
		}

		public bool Show(PropertyInformation property)
		{
			// use a regular expression if we have one and we also have a filter string.
			if (this.filterRegex != null && !string.IsNullOrEmpty(this.FilterString))
			{
				return
				(
					this.filterRegex.IsMatch(property.DisplayName) ||
					property.Property != null && this.filterRegex.IsMatch(property.Property.PropertyType.Name)
				);
			}
			// else just check for containment if we don't have a regular expression but we do have a filter string.
			else if (!string.IsNullOrEmpty(this.FilterString))
			{
				if (property.DisplayName.ToLower().Contains(this.FilterString))
					return true;
				if (property.Property != null && property.Property.PropertyType.Name.ToLower().Contains(this.FilterString))
					return true;
				return false;
			}
			// else use the filter set if we have one of those.
			else if (IsPropertyFilterSet)
			{
				if (SelectedFilterSet.IsPropertyInFilter(property.DisplayName))
					return true;
				else
					return false;
			}
			// finally, if none of the above applies
			// just check to see if we're not showing properties at their default values
			// and this property is actually set to its default value
			else
			{
				if (!this.ShowDefaults && property.ValueSource.BaseValueSource == BaseValueSource.Default)
					return false;
				else
					return true;
			}
		}
	}

	[Serializable]
	public class PropertyFilterSet
	{
		public string DisplayName
		{
			get;
			set;
		}

		public bool IsDefault
		{
			get;
			set;
		}

		public bool IsEditCommand
		{
			get;
			set;
		}

		public string[] Properties
		{
			get;
			set;
		}

		public bool IsPropertyInFilter(string property)
		{
			string lowerProperty = property.ToLower();
			foreach (var filterProp in Properties)
			{
				if (lowerProperty.StartsWith(filterProp))
				{
					return true;
				}
			}
			return false;
		}
	}
}
