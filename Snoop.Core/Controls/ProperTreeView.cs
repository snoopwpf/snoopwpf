// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Snoop.Data.Tree;
    using Snoop.Infrastructure;
    using Snoop.Windows;

    public class ProperTreeView : TreeView
    {
        private const int MaxExtentWidth = 1200;
        private const int ItemsToShowAboveNewRoot = 10;

        private SnoopUI? snoopUI;
        private ScrollViewer? scrollViewer;

        // We need this method and what it does because:
        // If we have a tree which causes the scroll viewer to exceed a certain extent width we might get an StackOverflowException during measure/arrange.
        // To prevent these Exceptions (which immediately crash the program being snooped) we use the currently selected item, minus a few, as the new root node for the tree.
        // That way we get a "new" tree that is not as deeply nested as before, thus reducing the extent width of the scroll viewer.
        // If the currently selected item is the top most item in the current tree, we revert some of the reduction and
        public bool ApplyReduceDepthFilterIfNeeded(ProperTreeViewItem curNode)
        {
            if (MaxExtentWidth == 0
                || curNode.IsSelected == false)
            {
                return false;
            }

            if (this.snoopUI is null)
            {
                this.snoopUI = Window.GetWindow(this) as SnoopUI;

                if (this.snoopUI is null)
                {
                    return false;
                }
            }

            if (curNode is null)
            {
                return false;
            }

            var item = (TreeItem)curNode.DataContext;

            if (item.Parent is null)
            {
                return false;
            }

            var rootItem = this.GetRootItem();

            if (rootItem is null)
            {
                return false;
            }

            var shouldReduce = this.scrollViewer is { ExtentWidth: >= MaxExtentWidth };
            var shouldWiden = shouldReduce == false && item == rootItem;

            if (shouldReduce == false
                && shouldWiden == false)
            {
                return false;
            }

            // Try to show some items above new root, that way we can keep a bit of context
            var newRoot = item.Parent;

            for (var i = 0; i < ItemsToShowAboveNewRoot; ++i)
            {
                if (newRoot?.Parent is null)
                {
                    break;
                }

                newRoot = newRoot.Parent;
            }

            this.snoopUI.ApplyReduceDepthFilter(newRoot);

            return true;
        }

        private TreeItem? GetRootItem()
        {
            return this.Items[0] as TreeItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ProperTreeViewItem(new WeakReference(this));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.scrollViewer = this.Template.FindName("_tv_scrollviewer_", this) as ScrollViewer;
        }
    }

    public class ProperTreeViewItem : TreeViewItem
    {
        private readonly WeakReference treeView;

        public ProperTreeViewItem(WeakReference treeView)
        {
            this.treeView = treeView;
        }

        public double Indent
        {
            get => (double)this.GetValue(IndentProperty);
            set => this.SetValue(IndentProperty, value);
        }

        /// <summary>Identifies the <see cref="Indent"/> dependency property.</summary>
        public static readonly DependencyProperty IndentProperty =
            DependencyProperty.Register(
                nameof(Indent),
                typeof(double),
                typeof(ProperTreeViewItem));

        protected override DependencyObject GetContainerForItemOverride()
        {
            var treeViewItem = new ProperTreeViewItem(this.treeView)
            {
                Indent = this.Indent + 12
            };
            return treeViewItem;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            // Check whether the tree is too deep.
            try
            {
                var targetTreeView = (ProperTreeView?)this.treeView.Target;
                if (targetTreeView is null
                    || targetTreeView.ApplyReduceDepthFilterIfNeeded(this) == false)
                {
                    return base.MeasureOverride(constraint);
                }
            }
            catch (Exception exception)
            {
                LogHelper.WriteWarning(exception);
            }

            return Size.Empty;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            // Check whether the tree is too deep.
            try
            {
                var targetTreeView = (ProperTreeView?)this.treeView.Target;
                if (targetTreeView is null
                    || targetTreeView.ApplyReduceDepthFilterIfNeeded(this) == false)
                {
                    return base.ArrangeOverride(arrangeBounds);
                }
            }
            catch (Exception exception)
            {
                LogHelper.WriteWarning(exception);
            }

            return Size.Empty;
        }
    }

    public class IndentToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Thickness((double)value, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}