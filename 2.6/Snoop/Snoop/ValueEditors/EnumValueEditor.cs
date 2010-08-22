// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
	using System;
	using System.Windows;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Windows.Data;

	public partial class EnumValueEditor: ValueEditor
	{
		private List<object> values = new List<object>();
		private bool isValid = false;
		private ListCollectionView valuesView;

		public EnumValueEditor() {
			this.valuesView = (ListCollectionView)CollectionViewSource.GetDefaultView(this.values);
			this.valuesView.CurrentChanged += this.HandleSelectionChanged;
		}

		public IList<object> Values {
			get { return this.values; }
		}

		protected override void OnTypeChanged() {
			base.OnTypeChanged();

			this.isValid = false;

			this.values.Clear();

			Type propertyType = this.PropertyType;
			if (propertyType != null) {

				Array values = Enum.GetValues(propertyType);
				foreach(object value in values) {
					this.values.Add(value);

					if (this.Value != null && this.Value.Equals(value))
						this.valuesView.MoveCurrentTo(value);
				}
			}

			this.isValid = true;
		}

		private void HandleSelectionChanged(object sender, EventArgs e) {
			if (this.isValid && this.Value != null) {
				if (!this.Value.Equals(this.valuesView.CurrentItem))
					this.Value = this.valuesView.CurrentItem;
			}
		}

		protected override void OnValueChanged(object newValue) {
			base.OnValueChanged(newValue);

			this.valuesView.MoveCurrentTo(newValue);
		}
	}
}
