// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls.ValueEditors
{
    using System;
    using System.Collections.ObjectModel;
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

        public ObservableCollection<object> Values { get; } = new();

        protected override void OnPropertyTypeChanged()
        {
            base.OnPropertyTypeChanged();

            this.isValid = false;

            this.Values.Clear();

            var propertyType = this.PropertyType;
            if (propertyType is not null)
            {
                var values = Enum.GetValues(propertyType);
                foreach (var value in values)
                {
                    if (value is null)
                    {
                        continue;
                    }

                    this.Values.Add(value);

                    if (this.Value is not null
                        && this.Value.Equals(value))
                    {
                        this.valuesView.MoveCurrentTo(value);
                    }
                }
            }

            this.isValid = true;
        }

        protected override void OnValueChanged(object? newValue)
        {
            base.OnValueChanged(newValue);

            this.valuesView.MoveCurrentTo(newValue);

            // sneaky trick here. only if both are non-null is this a change
            // caused by the user. If so, set the bool to track it.
            if (this.PropertyInfo is not null
                && newValue is not null)
            {
                this.PropertyInfo.IsValueChangedByUser = true;
            }
        }

        private void HandleSelectionChanged(object? sender, EventArgs e)
        {
            if (this.isValid
                && this.Value is not null)
            {
                if (!this.Value.Equals(this.valuesView.CurrentItem))
                {
                    this.Value = this.valuesView.CurrentItem;
                }
            }
        }
    }
}