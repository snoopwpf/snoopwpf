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

    public class TreeItemDragDropData
    {
        public Visual DraggedVisual { get; set; }
        public ResourceContainerItem DataItem { get; set; }
    }

    public class DragAdorner : System.Windows.Documents.Adorner
    {
        public DragAdorner(UIElement adornedElement, Point offset)
            : base(adornedElement)
        {

            this.offset = offset;
            vbrush = new VisualBrush(AdornedElement);
            vbrush.Opacity = .7;
            this.IsHitTestVisible = false;
        }

        public void UpdatePosition(Point location)
        {
            this.location = location;
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var p = location;

            p.Offset(-offset.X, -offset.Y);
            //var p = offset;
            System.Diagnostics.Debug.WriteLine(string.Format("onrender p = {0}", p));
            dc.DrawRectangle(vbrush, null, new Rect(p, this.RenderSize));
        }

        private Brush vbrush;
        private Point location;
        private Point offset;
    }

    public abstract class DragDropBehavior<T, ContainerType>
        where T : Control
        where ContainerType : Control
    {
        public void Attach(T item)
        {
            _item = item;
            item.AllowDrop = true;
            item.Drop += ProperTreeViewItem_Drop;
            item.MouseMove += ProperTreeViewItem_MouseMove;
            item.DragOver += ProperTreeViewItem_DragOver;
            item.DragLeave += ProperTreeViewItem_DragLeave;

        }

        protected T _item;

        protected abstract object PackageDataForDrag();

        protected abstract void OnDrop(DragEventArgs e);

        private void ProperTreeViewItem_Drop(object sender, DragEventArgs e)
        {
            _item.Background = Brushes.Transparent;
            OnDrop(e);
            e.Handled = true;
        }

        private static DragAdorner _dragAdorner;
        private void ProperTreeViewItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var ellipse = sender as FrameworkElement;

            if (ellipse != null && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                object data = PackageDataForDrag();

                var item = VisualTreeHelper2.GetAncestor<ContainerType>(_item);
                Point p = e.GetPosition(item);
                AddAddornerToTree(p);
                DragDrop.DoDragDrop(ellipse,
                                     data,
                                     DragDropEffects.All);
                RemoveAddornerFromTree();
            }

            e.Handled = true;
        }


        private void ProperTreeViewItem_DragLeave(object sender, DragEventArgs e)
        {
            _item.Background = Brushes.Transparent;
            e.Handled = true;
        }

        private void ProperTreeViewItem_DragOver(object sender, DragEventArgs e)
        {
            _item.Background = Brushes.Yellow;
            var containerElement = VisualTreeHelper2.GetAncestor<ContainerType>(_item);
            if (_dragAdorner != null)
            {
                _dragAdorner.UpdatePosition(e.GetPosition(containerElement));
            }
            var point = System.Windows.Input.Mouse.GetPosition(containerElement);
            e.Handled = true;
        }

        private void AddAddornerToTree(Point p)
        {
            var treeView = VisualTreeHelper2.GetAncestor<TreeView>(_item);
            if (treeView != null)
            {
                var adornerLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(treeView);
                _dragAdorner = new DragAdorner(_item, p);
                adornerLayer.Add(_dragAdorner);

            }
        }

        private void RemoveAddornerFromTree()
        {
            var treeView = VisualTreeHelper2.GetAncestor<TreeView>(_item);
            if (treeView != null)
            {
                var adornerLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(treeView);
                adornerLayer.Remove(_dragAdorner);
            }
        }
    }

    public class SnoopDragDropBehavior : DragDropBehavior<ProperTreeViewItem, TreeView>
    {
        protected override object PackageDataForDrag()
        {
            TreeItemDragDropData data = new TreeItemDragDropData();
            data.DraggedVisual = ((ResourceContainerItem)_item.DataContext).MainVisual;
            data.DataItem = ((ResourceContainerItem)_item.DataContext);
            return data;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            object[] data = (object[])e.Data.GetData(typeof(object[]));
            TreeItemDragDropData treeItemData = null;
            if ((treeItemData = e.Data.GetData(typeof(TreeItemDragDropData)) as TreeItemDragDropData) != null)
            {
                TreeItemDrop(treeItemData);
            }
        }

        private void TreeItemDrop(TreeItemDragDropData treeItemData)
        {
            //ProperTreeViewItem droppedTreeItem = (ProperTreeViewItem)sender;
            var draggedVisual = (FrameworkElement)treeItemData.DraggedVisual;
            var droppedVisual = (FrameworkElement)((VisualTreeItem)_item.DataContext).MainVisual;
            var panel = draggedVisual.Parent as Panel;
            if (panel == null)
                return;
            var targetPanel = droppedVisual as Panel;
            if (targetPanel == null)
                return;
            if (targetPanel == draggedVisual)
                return;
            panel.Children.Remove(draggedVisual);
            targetPanel.Children.Add(draggedVisual);

            VisualTreeItem highestParent = treeItemData.DataItem;
            while (highestParent.Parent != null)
                highestParent = highestParent.Parent;

            highestParent.Reload();
        }

    }

	public class ProperTreeViewItem : TreeViewItem
	{
        SnoopDragDropBehavior _dragDropBehavior = new SnoopDragDropBehavior();
		public ProperTreeViewItem(WeakReference treeView)
		{
			_treeView = treeView;
            _dragDropBehavior.Attach(this);
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
