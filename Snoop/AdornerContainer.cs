// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace Snoop
{
	/// <summary>
	/// Simple helper class to allow any UIElements to be used as an Adorner.
	/// </summary>
	public class AdornerContainer : Adorner
	{
		public AdornerContainer(UIElement adornedElement): base(adornedElement)
		{
		}

		protected override int VisualChildrenCount
		{
			get { return this.child == null ? 0 : 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			if (index == 0 && this.child != null)
				return this.child;
			return base.GetVisualChild(index);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.child != null)
				this.child.Arrange(new Rect(finalSize));
			return finalSize;
		}

		public UIElement Child
		{
			get { return this.child; }
			set
			{
				this.AddVisualChild(value);
				this.child = value;
			}
		}
		private UIElement child;
	}
}
