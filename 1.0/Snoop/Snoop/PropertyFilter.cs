namespace Snoop
{
	using System.Windows;

	public class PropertyFilter {
		private string filterString;
		private bool showDefaults;

		public PropertyFilter(string filterString, bool showDefaults) {
			this.filterString = filterString.ToLower();
			this.showDefaults = showDefaults;
		}

		public string FilterString {
			get { return this.filterString; }
			set { this.filterString = value.ToLower(); }
		}

		public bool ShowDefaults {
			get { return this.showDefaults; }
			set { this.showDefaults = value; }
		}

		public bool Show(PropertyInformation property) {
			if (string.IsNullOrEmpty(this.filterString)) {
				if (!this.ShowDefaults && property.ValueSource.BaseValueSource == BaseValueSource.Default)
					return false;
				return true;
			}

			if (property.DisplayName.ToLower().Contains(this.FilterString))
				return true;
			if (property.Property.PropertyType.Name.ToLower().Contains(this.FilterString))
				return true;
			if (property.Property.ComponentType.Name.ToLower().Contains(this.FilterString))
				return true;
			return false;
		}
	}
}
