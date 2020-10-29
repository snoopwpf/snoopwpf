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
    using System.Windows.Media;
    using Snoop.Data.Tree;
    using Snoop.Infrastructure.Helpers;
    using Snoop.Windows;

    public class ProperTreeView : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ProperTreeViewItem(new WeakReference(this));
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