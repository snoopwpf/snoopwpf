// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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

namespace Snoop
{
	public class VisualTreeItem : INotifyPropertyChanged
	{
		public static VisualTreeItem Construct(object target, VisualTreeItem parent)
		{
			VisualTreeItem visualTreeItem;

			if (target is Visual)
				visualTreeItem = new VisualItem((Visual)target, parent);
			else if (target is ResourceDictionary)
				visualTreeItem = new ResourceDictionaryItem((ResourceDictionary)target, parent);
			else if (target is Application)
				visualTreeItem = new ApplicationTreeItem((Application)target, parent);
			else
				visualTreeItem = new VisualTreeItem(target, parent);

			visualTreeItem.Reload();

			return visualTreeItem;
		}
		protected VisualTreeItem(object target, VisualTreeItem parent)
		{
		    if (target == null) throw new ArgumentNullException("target");
		    this.target = target;
			this.parent = parent;
			if (parent != null)
				this.depth = parent.depth + 1;
		}


		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(50);

			// [depth] name (type) numberOfChildren
			sb.AppendFormat("[{0}] {1} ({2})", this.depth.ToString("D3"), this.name, this.Target.GetType().Name);
			if (this.visualChildrenCount != 0)
			{
				sb.Append(' ');
				sb.Append(this.visualChildrenCount.ToString());
			}

			return sb.ToString();
		}


		/// <summary>
		/// The WPF object that this VisualTreeItem is wrapping
		/// </summary>
		public object Target
		{
			get { return this.target; }
		}
		private object target;

		/// <summary>
		/// The VisualTreeItem parent of this VisualTreeItem
		/// </summary>
		public VisualTreeItem Parent
		{
			get { return this.parent; }
		}
		private VisualTreeItem parent;

		/// <summary>
		/// The depth (in the visual tree) of this VisualTreeItem
		/// </summary>
		public int Depth
		{
			get { return this.depth; }
		}
		private int depth;

		/// <summary>
		/// The VisualTreeItem children of this VisualTreeItem
		/// </summary>
		public ObservableCollection<VisualTreeItem> Children
		{
			get { return this.children; }
		}
		private ObservableCollection<VisualTreeItem> children = new ObservableCollection<VisualTreeItem>();


		public bool IsSelected
		{
			get { return this.isSelected; }
			set
			{
				if (this.isSelected != value)
				{
					this.isSelected = value;

					// Need to expand all ancestors so this will be visible in the tree.
					if (this.isSelected && this.parent != null)
						this.parent.ExpandTo();

					this.OnPropertyChanged("IsSelected");
					this.OnSelectionChanged();
				}
			}
		}
		protected virtual void OnSelectionChanged()
		{
		}
		private bool isSelected = false;

		/// <summary>
		/// Need this to databind to TreeView so we can display to hidden items.
		/// </summary>
		public bool IsExpanded
		{
			get { return this.isExpanded; }
			set
			{
				if (this.isExpanded != value)
				{
					this.isExpanded = value;
					this.OnPropertyChanged("IsExpanded");
				}
			}
		}
		/// <summary>
		/// Expand this element and all elements leading to it.
		/// Used to show this element in the tree view.
		/// </summary>
		private void ExpandTo()
		{
			if (this.parent != null)
				this.parent.ExpandTo();

			this.IsExpanded = true;
		}
		private bool isExpanded = false;


		public virtual Visual MainVisual
		{
			get { return null; }
		}
		public virtual Brush TreeBackgroundBrush
		{
			get { return new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)); }
		}
		public virtual Brush VisualBrush
		{
			get { return null; }
		}
		/// <summary>
		/// Checks to see if any property on this element has a binding error.
		/// </summary>
		public virtual bool HasBindingError
		{
			get
			{
				return false;
			}
		}


		/// <summary>
		/// Update the view of this visual, rebuild children as necessary
		/// </summary>
		public void Reload()
		{
			this.name = (this.target is FrameworkElement) ? ((FrameworkElement)this.target).Name : string.Empty;

			this.nameLower = (this.name ?? "").ToLower();
			this.typeNameLower = this.Target != null ? this.Target.GetType().Name.ToLower() : string.Empty;

			List<VisualTreeItem> toBeRemoved = new List<VisualTreeItem>(this.Children);
			this.Reload(toBeRemoved);
			foreach (VisualTreeItem item in toBeRemoved)
				this.RemoveChild(item);


			// calculate the number of visual children
			foreach (VisualTreeItem child in this.Children)
			{
				if (child is VisualItem)
					this.visualChildrenCount++;

				this.visualChildrenCount += child.visualChildrenCount;
			}
		}
		protected virtual void Reload(List<VisualTreeItem> toBeRemoved)
		{
		}



		public VisualTreeItem FindNode(object target)
		{
			// it might be faster to have a map for the lookup
			// check into this at some point

			if (this.Target == target)
				return this;

			foreach (VisualTreeItem child in this.Children)
			{
				VisualTreeItem node = child.FindNode(target);
				if (node != null)
					return node;
			}
			return null;
		}


		/// <summary>
		/// Used for tree search.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool Filter(string value)
		{
			if (this.typeNameLower.Contains(value))
				return true;
			if (this.nameLower.Contains(value))
				return true;
			int n;
			if (int.TryParse(value, out n) && n == this.depth)
				return true;
			return false;
		}


		protected void RemoveChild(VisualTreeItem item)
		{
			item.IsSelected = false;
			this.Children.Remove(item);
		}


		private string name;
		private string nameLower = string.Empty;
		private string typeNameLower = string.Empty;
		private int visualChildrenCount = 0;


		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
