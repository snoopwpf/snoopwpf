// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace Snoop
{
	public partial class EnumValueEditor : ValueEditor
	{
		public EnumValueEditor()
		{
			this.valuesView = (ListCollectionView)CollectionViewSource.GetDefaultView(this.values);
			this.valuesView.CurrentChanged += this.HandleSelectionChanged;
		}


		public IList<object> Values
		{
			get { return this.values; }
		}
		private List<object> values = new List<object>();


		protected override void OnTypeChanged()
		{
			base.OnTypeChanged();

			this.isValid = false;

			this.values.Clear();

			Type propertyType = this.PropertyType;
			if (propertyType != null)
			{
				Array values = Enum.GetValues(propertyType);
				foreach(object value in values)
				{
					this.values.Add(value);

					if (this.Value != null && this.Value.Equals(value))
						this.valuesView.MoveCurrentTo(value);
				}
			}

			this.isValid = true;
		}

		protected override void OnValueChanged(object newValue)
		{
			base.OnValueChanged(newValue);

			this.valuesView.MoveCurrentTo(newValue);

			// sneaky trick here.  only if both are non-null is this a change
			// caused by the user.  If so, set the bool to track it.
			if ( PropertyInfo != null && newValue != null )
			{
				PropertyInfo.IsValueChangedByUser = true;
			}
		}


		private void HandleSelectionChanged(object sender, EventArgs e)
		{
			if (this.isValid && this.Value != null)
			{
				if (!this.Value.Equals(this.valuesView.CurrentItem))
					this.Value = this.valuesView.CurrentItem;
			}
		}


		private bool isValid = false;
		private ListCollectionView valuesView;
	}
}
