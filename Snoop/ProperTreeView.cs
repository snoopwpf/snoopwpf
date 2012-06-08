// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Diagnostics;

namespace Snoop
{
	public class ProperTreeView : TreeView
	{
		public bool ApplyReduceDepthFilterIfNeeded(ProperTreeViewItem curNode)
		{
			if (_pendingRoot != null)
			{
				OnRootLoaded();
			}

			if (_maxDepth == 0)
			{
				return false;
			}

			VisualTreeItem rootItem = (VisualTreeItem)_rootItem.Target;
			if (rootItem == null)
			{
				return false;
			}

			if (_snoopUI == null)
			{
				_snoopUI = VisualTreeHelper2.GetAncestor<SnoopUI>(this);
				if (_snoopUI == null)
				{
					return false;
				}
			}

			VisualTreeItem item = (VisualTreeItem)curNode.DataContext;
			VisualTreeItem selectedItem = _snoopUI.CurrentSelection;
			if (selectedItem != null && item.Depth < selectedItem.Depth)
			{
				item = selectedItem;
			}

			if ((item.Depth - rootItem.Depth) <= _maxDepth)
			{
				return false;
			}

			for (int i = 0; i < _maxDepth; ++i)
			{
				item = item.Parent;
			}

			_snoopUI.ApplyReduceDepthFilter(item);
			return true;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			if (_pendingRoot != null)
			{
				_pendingRoot.Loaded -= OnRootLoaded;
				_pendingRoot = null;
			}
			_pendingRoot = new ProperTreeViewItem(new WeakReference(this));
			_pendingRoot.Loaded += OnRootLoaded;
			_maxDepth = 0;
			_rootItem.Target = null;
			return _pendingRoot;
		}

		private void OnRootLoaded(object sender, RoutedEventArgs e)
		{
			Debug.Assert(_pendingRoot == sender, "_pendingRoot == sender");
			OnRootLoaded();
		}
		private void OnRootLoaded()
		{
			// The following assumptions are made:
			// 1. The visual structure of each TreeViewItem is the same regardless of its location.
			// 2. The control template of a TreeViewItem contains ItemsPresenter.
			ProperTreeViewItem root = _pendingRoot;

			_pendingRoot = null;
			root.Loaded -= OnRootLoaded;

			ItemsPresenter itemsPresenter = null;
			VisualTreeHelper2.EnumerateTree(root, null,
				delegate(Visual visual, object misc)
				{
					itemsPresenter = visual as ItemsPresenter;
					if (itemsPresenter != null && itemsPresenter.TemplatedParent == root)
					{
						return HitTestResultBehavior.Stop;
					}
					else
					{
						itemsPresenter = null;
						return HitTestResultBehavior.Continue;
					}
				},
				null);

			if (itemsPresenter != null)
			{
				int levelLayoutDepth = 2;
				DependencyObject tmp = itemsPresenter;
				while (tmp != root)
				{
					++levelLayoutDepth;
					tmp = VisualTreeHelper.GetParent(tmp);
				}

				int rootLayoutDepth = 0;
				while (tmp != null)
				{
					++rootLayoutDepth;
					tmp = VisualTreeHelper.GetParent(tmp);
				}

				_maxDepth = (240 - rootLayoutDepth) / levelLayoutDepth;
				_rootItem = new WeakReference((VisualTreeItem)root.DataContext);
			}
		}

		private int _maxDepth;
		private SnoopUI _snoopUI;
		private ProperTreeViewItem _pendingRoot;
		private WeakReference _rootItem = new WeakReference(null);
	}

	public class ProperTreeViewItem : TreeViewItem
	{
		public ProperTreeViewItem(WeakReference treeView)
		{
			_treeView = treeView;
		}

		public double Indent
		{
			get { return (double)this.GetValue(ProperTreeViewItem.IndentProperty); }
			set { this.SetValue(ProperTreeViewItem.IndentProperty, value); }
		}
		public static readonly DependencyProperty IndentProperty =
			DependencyProperty.Register
			(
				"Indent",
				typeof(double),
				typeof(ProperTreeViewItem)
			);

		protected override void OnSelected(RoutedEventArgs e)
		{
			// scroll the selection into view
			BringIntoView();

			base.OnSelected(e);
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			ProperTreeViewItem treeViewItem = new ProperTreeViewItem(_treeView);
			treeViewItem.Indent = this.Indent + 12;
			return treeViewItem;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			// Check whether the tree is too deep.
			try
			{
				ProperTreeView treeView = (ProperTreeView)_treeView.Target;
				if (treeView == null || !treeView.ApplyReduceDepthFilterIfNeeded(this))
				{
					return base.MeasureOverride(constraint);
				}
			}
			catch
			{
			}
			return new Size(0, 0);
		}
		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			// Check whether the tree is too deep.
			try
			{
				ProperTreeView treeView = (ProperTreeView)_treeView.Target;
				if (treeView == null || !treeView.ApplyReduceDepthFilterIfNeeded(this))
				{
					return base.ArrangeOverride(arrangeBounds);
				}
			}
			catch
			{
			}
			return new Size(0, 0);
		}

		private WeakReference _treeView;
	}

	public class IndentToMarginConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new Thickness((double)value, 0, 0, 0);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
