// Copyright © 2006 Microsoft Corporation.  All Rights Reserved

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
    using System.Text;

	public class VisualTreeItem: INotifyPropertyChanged {

		private ObservableCollection<VisualTreeItem> children = new ObservableCollection<VisualTreeItem>();
		private object target;
		private string name;
		private string nameLower = string.Empty;
		private string typeNameLower = string.Empty;
		private bool isSelected = false;
		private bool isExpanded = false;
        private int depth;

		private VisualTreeItem parent;
		private int visualChildrenCount;

		protected VisualTreeItem(object target, VisualTreeItem parent) {
			this.target = target;
			this.parent = parent;
            if (parent != null) {
                this.depth = parent.depth + 1;
            }
        }

        public int Depth {
            get { return this.depth; }
        }

        public VisualTreeItem Parent {
            get { return this.parent; }
        }

		public object Target {
			get { return this.target; }
		}

		public ObservableCollection<VisualTreeItem> Children {
			get { return this.children; }
		}

		/// <summary>
		/// Update the view of this visual, rebuild children as necessary
		/// </summary>
		public void Reload() {
			this.name = (this.target is FrameworkElement) ? ((FrameworkElement)this.target).Name : string.Empty;

			this.nameLower = this.name.ToLower();
			this.typeNameLower = this.Target.GetType().Name.ToLower();

			List<VisualTreeItem> toBeRemoved = new List<VisualTreeItem>(this.Children);
			this.Reload(toBeRemoved);

			foreach (VisualTreeItem item in toBeRemoved)
				this.RemoveChild(item);
		}

		protected virtual void Reload(List<VisualTreeItem> toBeRemoved) {
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

		public virtual Visual MainVisual {
			get { return null; }
		}

		public virtual Brush TreeBackgroundBrush {
			get { return new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)); }
		}

		public virtual Brush VisualBrush {
			get { return null; }
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
            StringBuilder sb = new StringBuilder(50);
            sb.AppendFormat("[{0}] {1} ({2})", this.depth.ToString("D3"), this.name, this.Target.GetType().Name);
            if (this.visualChildrenCount != 0)
            {
                sb.Append(' ');
                sb.Append(this.visualChildrenCount.ToString());
            }
            return sb.ToString();
		}

		// Might be faster to have a map...
		public VisualTreeItem FindNode(object target) {
			if (this.Target == target)
				return this;

			foreach (VisualTreeItem child in this.Children) {
				VisualTreeItem node = child.FindNode(target);
				if (node != null)
					return node;
			}
			return null;
		}

		public int UpdateVisualChildrenCount() {
			this.visualChildrenCount = 0;
			foreach (VisualTreeItem child in this.Children) {
				if (child is VisualItem)
					this.visualChildrenCount += child.UpdateVisualChildrenCount();
			}
			if (this is VisualItem)
				return this.visualChildrenCount + 1;

			return this.visualChildrenCount;
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
            int n;
            if (int.TryParse(value, out n) && n == this.depth)
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

		public static VisualTreeItem Construct(object target, VisualTreeItem parent) {
			VisualTreeItem item;
			if (target is Visual)
				item = new VisualItem((Visual)target, parent);
			else if (target is ResourceDictionary)
				item = new ResourceDictionaryItem((ResourceDictionary)target, parent);
			else if (target is Application)
				item = new ApplicationTreeItem((Application)target, parent);
			else
				item = new VisualTreeItem(target, parent);

			item.Reload();
			return item;
		}

		/// <summary>
		/// Checks to see if any property on this element has a binding error.
		/// </summary>
		public virtual bool HasBindingError {
			get {
				return false;
			}
		}

		protected void RemoveChild(VisualTreeItem item) {
			item.IsSelected = false;
			this.Children.Remove(item);
		}
	}
}
