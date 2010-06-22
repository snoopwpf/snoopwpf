namespace Snoop
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Windows;
	using System.Windows.Media;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Windows.Data;
	using System;
	using System.Windows.Controls;

	public class VisualTreeItem: INotifyPropertyChanged {

		private ObservableCollection<VisualTreeItem> children = new ObservableCollection<VisualTreeItem>();
		private DependencyObject target;
		private string name;
		private string nameLower = string.Empty;
		private string typeName;
		private string typeNameLower = string.Empty;
		private bool isSelected = false;
		private bool isExpanded = false;

		private VisualTreeItem parent;
		private int totalChildren;

		protected VisualTreeItem(DependencyObject target, VisualTreeItem parent) {
			this.target = target;
			this.parent = parent;
		}

		public DependencyObject Target {
			get { return this.target; }
		}

		public ObservableCollection<VisualTreeItem> Children {
			get { return this.children; }
		}

		/// <summary>
		/// Update the view of this visual, rebuild children as necessary
		/// </summary>
		public void Reload()
		{
			this.name = (this.target is FrameworkElement) ? ((FrameworkElement)this.target).Name : string.Empty;
			this.typeName = this.target.GetType().Name;

			this.nameLower = this.name.ToLower();
			this.typeNameLower = this.typeName.ToLower();

			List<VisualTreeItem> toBeRemoved = new List<VisualTreeItem>(this.children);

			if (!(this.target is RowDefinition) && !(this.target is ColumnDefinition))
			{
				// Remove items that are no longer in tree, add new ones.
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(this.target); i++)
				{

					DependencyObject child = VisualTreeHelper.GetChild(this.target, i);
					if (child != null)
					{

						bool foundItem = false;
						foreach (VisualTreeItem item in toBeRemoved)
						{
							if (item.Target == child)
							{
								toBeRemoved.Remove(item);
								item.Reload();
								foundItem = true;
								break;
							}
						}
						if (!foundItem)
							this.Children.Add(VisualTreeItem.Construct(child, this));
					}
				}
			}

			Grid grid = this.target as Grid;
			if (grid != null)
			{
				foreach (RowDefinition row in grid.RowDefinitions)
					this.Children.Add(VisualTreeItem.Construct(row, this));
				foreach (ColumnDefinition column in grid.ColumnDefinitions)
					this.Children.Add(VisualTreeItem.Construct(column, this));
			}

			foreach (VisualTreeItem item in toBeRemoved)
			{
				item.IsSelected = false;
				this.children.Remove(item);
			}
		}

		public bool IsSelected {
			get { return this.isSelected; }
			set {
				if (this.isSelected != value) {
					this.isSelected = value;
					// Need to expand all ancestors so this will be visible in the tree.
					if (this.isSelected && this.parent != null)
						this.parent.ExpandTo();

					this.OnPropertyChanged("IsSelected");
					this.OnSelectionChanged();
				}
			}
		}

		/// <summary>
		/// Need this to databind to TreeView so we can display to hidden items.
		/// </summary>
		public bool IsExpanded {
			get { return this.isExpanded; }
			set {
				if (this.isExpanded != value) {
					this.isExpanded = value;
					this.OnPropertyChanged("IsExpanded");
				}
			}
		}

		/// <summary>
		/// Expand this element and all elements leading to it.
		/// Used to show this element in the tree view.
		/// </summary>
		private void ExpandTo() {
			if (this.parent != null)
				this.parent.ExpandTo();

			this.IsExpanded = true;
		}

		public override string ToString() {
			return this.name + " (" + this.typeName + ") " + this.totalChildren;
		}

		// Might be faster to have a map...
		public VisualTreeItem FindNode(DependencyObject target) {
			if (this.Target == target)
				return this;

			foreach (VisualTreeItem child in this.Children) {
				VisualTreeItem node = child.FindNode(target);
				if (node != null)
					return node;
			}
			return null;
		}

		public int UpdateChildrenCount() {
			this.totalChildren = 0;
			foreach (VisualTreeItem child in this.Children)
				this.totalChildren += child.UpdateChildrenCount();
			return this.totalChildren + 1;
		}

		/// <summary>
		/// Used for tree search.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool Filter(string value) {
			if (this.typeNameLower.Contains(value))
				return true;
			if (this.nameLower.Contains(value))
				return true;
			return false;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnSelectionChanged() {
		}

		public static VisualTreeItem Construct(DependencyObject target, VisualTreeItem parent) {
			VisualTreeItem item;
			if (target is Visual)
				item = new VisualItem((Visual)target, parent);
			else
				item = new VisualTreeItem(target, parent);

			item.Reload();
			return item;
		}

		/// <summary>
		/// Checks to see if any property on this element has a binding error.
		/// </summary>
		public bool HasBindingError {
			get {
				//foreach (PropertyInformation prop in PropertyInformation.GetProperties(this.target)) {
				//    if (prop.IsInvalidBinding)
				//        return true;
				//}
				PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(this.target, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });
				foreach (PropertyDescriptor property in propertyDescriptors) {
				    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property);
				    if (dpd != null) {
				        BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(this.target, dpd.DependencyProperty);
				        if (expression != null && (expression.HasError || expression.Status != BindingStatus.Active))
				            return true;
				    }
				}
				//LocalValueEnumerator locals = this.target.GetLocalValueEnumerator();
				//while (locals.MoveNext())
				//{
				//    LocalValueEntry entry = locals.Current;

				//    BindingExpressionBase expression = entry.Value as BindingExpressionBase;
				//    if (expression != null && (expression.HasError || expression.Status != BindingStatus.Active))
				//        return true;
				//}
				return false;
			}
		}
	}
}
