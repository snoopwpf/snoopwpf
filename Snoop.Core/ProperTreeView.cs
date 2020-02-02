// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;

    public class ProperTreeView : TreeView
    {
        public bool ApplyReduceDepthFilterIfNeeded(ProperTreeViewItem curNode)
        {
            if (this.pendingRoot != null)
            {
                this.OnRootLoaded();
            }

            if (this.maxDepth == 0)
            {
                return false;
            }

            var rootItem = (TreeItem)this.rootItem.Target;
            if (rootItem == null)
            {
                return false;
            }

            if (this.snoopUI == null)
            {
                this.snoopUI = VisualTreeHelper2.GetAncestor<SnoopUI>(this);
                if (this.snoopUI == null)
                {
                    return false;
                }
            }

            var item = (TreeItem)curNode.DataContext;
            var selectedItem = this.snoopUI.CurrentSelection;
            if (selectedItem != null && item.Depth < selectedItem.Depth)
            {
                item = selectedItem;
            }

            if (item.Depth - rootItem.Depth <= this.maxDepth)
            {
                return false;
            }

            for (var i = 0; i < this.maxDepth; ++i)
            {
                item = item.Parent;
            }

            this.snoopUI.ApplyReduceDepthFilter(item);
            return true;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            if (this.pendingRoot != null)
            {
                this.pendingRoot.Loaded -= this.OnRootLoaded;
                this.pendingRoot = null;
            }

            this.pendingRoot = new ProperTreeViewItem(new WeakReference(this));
            this.pendingRoot.Loaded += this.OnRootLoaded;
            this.maxDepth = 0;
            this.rootItem.Target = null;
            return this.pendingRoot;
        }

        private void OnRootLoaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(this.pendingRoot == sender, "pendingRoot == sender");
            this.OnRootLoaded();
        }

        private void OnRootLoaded()
        {
            // The following assumptions are made:
            // 1. The visual structure of each TreeViewItem is the same regardless of its location.
            // 2. The control template of a TreeViewItem contains ItemsPresenter.
            var root = this.pendingRoot;

            this.pendingRoot = null;
            root.Loaded -= this.OnRootLoaded;

            ItemsPresenter itemsPresenter = null;
            VisualTreeHelper2.EnumerateTree(root, null,
                (visual, misc) =>
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
                var levelLayoutDepth = 2;
                DependencyObject tmp = itemsPresenter;
                while (tmp != root)
                {
                    ++levelLayoutDepth;
                    tmp = VisualTreeHelper.GetParent(tmp);
                }

                var rootLayoutDepth = 0;
                while (tmp != null)
                {
                    ++rootLayoutDepth;
                    tmp = VisualTreeHelper.GetParent(tmp);
                }

                this.maxDepth = (240 - rootLayoutDepth) / levelLayoutDepth;
                this.rootItem = new WeakReference((TreeItem)root.DataContext);
            }
        }

        private int maxDepth;
        private SnoopUI snoopUI;
        private ProperTreeViewItem pendingRoot;
        private WeakReference rootItem = new WeakReference(null);
    }

    public class ProperTreeViewItem : TreeViewItem
    {
        public ProperTreeViewItem(WeakReference treeView)
        {
            this.treeView = treeView;
        }

        public double Indent
        {
            get { return (double)this.GetValue(IndentProperty); }
            set { this.SetValue(IndentProperty, value); }
        }

        public static readonly DependencyProperty IndentProperty =
            DependencyProperty.Register(
                nameof(Indent),
                typeof(double),
                typeof(ProperTreeViewItem));

        protected override void OnSelected(RoutedEventArgs e)
        {
            // scroll the selection into view
            this.BringIntoView();

            base.OnSelected(e);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var treeViewItem = new ProperTreeViewItem(this.treeView);
            treeViewItem.Indent = this.Indent + 12;
            return treeViewItem;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            // Check whether the tree is too deep.
            try
            {
                var treeView = (ProperTreeView)this.treeView.Target;
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
                var treeView = (ProperTreeView)this.treeView.Target;
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

        private readonly WeakReference treeView;
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