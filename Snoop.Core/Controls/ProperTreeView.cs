// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Snoop.Data.Tree;
    using Snoop.Windows;

    public class ProperTreeView : TreeView
    {
        private readonly int maxDepth = 100;

        private SnoopUI? snoopUI;

        // We need this method and what it does because:
        // If we have a tree with levels greater than 150 then we might get an StackOverflowException during measure/arrange.
        // To prevent these Exceptions (which immediately crash the program being snooped) we use the item at the current level (minus a few) as the new root node for the tree.
        // That way we get a "new" tree that is not as deeply nested as before.
        public bool ApplyReduceDepthFilterIfNeeded(ProperTreeViewItem curNode)
        {
            if (this.maxDepth == 0)
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

            var item = (TreeItem)curNode.DataContext;
            var selectedItem = this.snoopUI.CurrentSelection;

            if (selectedItem is not null
                && item.Depth < selectedItem.Depth)
            {
                item = selectedItem;
            }

            var rootItem = this.GetRootItem();

            if (rootItem is null)
            {
                return false;
            }

            // Do we exceed the current max depth?
            if (item.Depth - rootItem.Depth <= this.maxDepth)
            {
                return false;
            }

            if (item.Parent is null)
            {
                return false;
            }

            // Try to show 10 items above new root, that way we can keep a bit of context
            var newRoot = item.Parent;
            for (var i = 0; i < 10; ++i)
            {
                if (newRoot?.Parent is null)
                {
                    break;
                }

                newRoot = newRoot.Parent;
            }

            this.snoopUI.ApplyReduceDepthFilter(newRoot!);

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
                var treeView = (ProperTreeView?)this.treeView.Target;
                if (treeView is null
                    || treeView.ApplyReduceDepthFilterIfNeeded(this) == false)
                {
                    return base.MeasureOverride(constraint);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceWarning(exception.ToString());
            }

            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            // Check whether the tree is too deep.
            try
            {
                var treeView = (ProperTreeView?)this.treeView.Target;
                if (treeView is null
                    || treeView.ApplyReduceDepthFilterIfNeeded(this) == false)
                {
                    return base.ArrangeOverride(arrangeBounds);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceWarning(exception.ToString());
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
            return Binding.DoNothing;
        }
    }
}