namespace Snoop
{

	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	/// <summary>
	/// Class to make TreeViewItems selectable across the entire row
	/// and to make them not scroll horizontally when selected.
	/// </summary>
	public class ProperTreeView: TreeView {

		protected override DependencyObject GetContainerForItemOverride() {
			return new ProperTreeViewItem();
		}
	}

	public class ProperTreeViewItem : TreeViewItem {
		public static readonly DependencyProperty IndentProperty = DependencyProperty.Register("Indent", typeof(double), typeof(ProperTreeViewItem));

		public double Indent {
			get { return (double)this.GetValue(ProperTreeViewItem.IndentProperty); }
			set { this.SetValue(ProperTreeViewItem.IndentProperty, value); }
		}

		protected override DependencyObject GetContainerForItemOverride() {
			ProperTreeViewItem treeViewItem = new ProperTreeViewItem();
			treeViewItem.Indent = this.Indent + 12;

			return treeViewItem;
		}
	}

	public class IndentToMarginConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return new Thickness((double)value, 0, 0, 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return null;
		}
	}
}
