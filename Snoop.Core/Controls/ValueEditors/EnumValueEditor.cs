// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Data;

    public class EnumValueEditor : ValueEditor
    {
        private readonly ListCollectionView valuesView;
        private bool isValid;

        public EnumValueEditor()
        {
            this.valuesView = (ListCollectionView)CollectionViewSource.GetDefaultView(this.Values);
            this.valuesView.CurrentChanged += this.HandleSelectionChanged;
        }

        public IList<object> Values { get; } = new List<object>();

        protected override void OnPropertyTypeChanged()
        {
            base.OnPropertyTypeChanged();

            this.isValid = false;

            this.Values.Clear();

            var propertyType = this.PropertyType;
            if (propertyType != null)
            {
                var values = Enum.GetValues(propertyType);
                foreach (var value in values)
                {
                    this.Values.Add(value);

                    if (this.Value != null
                        && this.Value.Equals(value))
                    {
                        this.valuesView.MoveCurrentTo(value);
                    }
                }
            }

            this.isValid = true;
        }

        protected override void OnValueChanged(object newValue)
        {
            base.OnValueChanged(newValue);

            this.valuesView.MoveCurrentTo(newValue);

            // sneaky trick here. only if both are non-null is this a change
            // caused by the user. If so, set the bool to track it.
            if (this.PropertyInfo != null
                && newValue != null)
            {
                this.PropertyInfo.IsValueChangedByUser = true;
            }
        }

        private void HandleSelectionChanged(object sender, EventArgs e)
        {
            if (this.isValid
                && this.Value != null)
            {
                if (!this.Value.Equals(this.valuesView.CurrentItem))
                {
                    this.Value = this.valuesView.CurrentItem;
                }
            }
        }
    }
}