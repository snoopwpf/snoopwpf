// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Data;

namespace Snoop
{
	/// <summary>
	/// Main class that represents a visual in the visual tree
	/// </summary>
	public class VisualItem : ResourceContainerItem
	{
		public VisualItem(Visual visual, VisualTreeItem parent): base(visual, parent)
		{
			this.visual = visual;
		}


		public Visual Visual
		{
			get { return this.visual; }
		}
		private Visual visual;


		public override bool HasBindingError
		{
			get
			{
				PropertyDescriptorCollection propertyDescriptors =
					TypeDescriptor.GetProperties(this.Visual, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });
				foreach (PropertyDescriptor property in propertyDescriptors)
				{
					DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property);
					if (dpd != null)
					{
						BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(this.Visual, dpd.DependencyProperty);
						if (expression != null && (expression.HasError || expression.Status != BindingStatus.Active))
							return true;
					}
				}
				return false;
			}
		}
		public override Visual MainVisual
		{
			get { return this.Visual; }
		}
		public override Brush TreeBackgroundBrush
		{
			get { return Brushes.Transparent; }
		}
		public override Brush VisualBrush
		{
			get
			{
				VisualBrush brush = new VisualBrush(this.Visual);
				brush.Stretch = Stretch.Uniform;
				return brush;
			}
		}


		protected override ResourceDictionary ResourceDictionary
		{
			get
			{
				FrameworkElement element = this.Visual as FrameworkElement;
				if (element != null)
					return element.Resources;
				return null;
			}
		}


		protected override void OnSelectionChanged()
		{
			// Add adorners for the visual this is representing.
			AdornerLayer adorners = AdornerLayer.GetAdornerLayer(this.Visual);
			UIElement visualElement = this.Visual as UIElement;

			if (adorners != null && visualElement != null)
			{
				if (this.IsSelected && this.adorner == null)
				{
					Border border = new Border();
					border.BorderThickness = new Thickness(4);

					Color borderColor = new Color();
					borderColor.ScA = .3f;
					borderColor.ScR = 1;
					border.BorderBrush = new SolidColorBrush(borderColor);

					border.IsHitTestVisible = false;
					this.adorner = new AdornerContainer(visualElement);
					adorner.Child = border;
					adorners.Add(adorner);
				}
				else if (this.adorner != null)
				{
					adorners.Remove(this.adorner);
					this.adorner.Child = null;
					this.adorner = null;
				}
			}
		}
		protected override void Reload(List<VisualTreeItem> toBeRemoved)
		{
			// having the call to base.Reload here ... puts the application resources at the very top of the tree view.
			// this used to be at the bottom. putting it here makes it consistent (and easier to use) with ApplicationTreeItem
			base.Reload(toBeRemoved);

			// remove items that are no longer in tree, add new ones.
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(this.Visual); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(this.Visual, i);
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

			Grid grid = this.Visual as Grid;
			if (grid != null)
			{
				foreach (RowDefinition row in grid.RowDefinitions)
					this.Children.Add(VisualTreeItem.Construct(row, this));
				foreach (ColumnDefinition column in grid.ColumnDefinitions)
					this.Children.Add(VisualTreeItem.Construct(column, this));
			}
		}


		private AdornerContainer adorner;
	}
}
