// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Snoop.Infrastructure;

namespace Snoop
{
	/// <summary>
	/// Interaction logic for ZoomerControl.xaml
	/// </summary>
	public partial class ZoomerControl : UserControl
	{
		public ZoomerControl()
		{
			InitializeComponent();

			this.transform.Children.Add(this.zoom);
			this.transform.Children.Add(this.translation);

			this.Viewbox.RenderTransform = this.transform;

//			DependencyPropertyDescriptor.FromProperty(TargetProperty, typeof(ZoomerControl)).AddValueChanged(this, TargetChanged);
		}


		#region Target
		/// <summary>
		/// Gets or sets the Target property.
		/// </summary>
		public object Target
		{
			get { return (object)GetValue(TargetProperty); }
			set { SetValue(TargetProperty, value); }
		}
		/// <summary>
		/// Target Dependency Property
		/// </summary>
		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.Register
			(
				"Target",
				typeof(object),
				typeof(ZoomerControl),
				new FrameworkPropertyMetadata
				(
					(object)null,
					new PropertyChangedCallback(OnTargetChanged)
				)
			);
		/// <summary>
		/// Handles changes to the Target property.
		/// </summary>
		private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((ZoomerControl)d).OnTargetChanged(e);
		}
		/// <summary>
		/// Provides derived classes an opportunity to handle changes to the Target property.
		/// </summary>
		protected virtual void OnTargetChanged(DependencyPropertyChangedEventArgs e)
		{
			ResetZoomAndTranslation();

			if (_pooSniffer == null)
				_pooSniffer = this.TryFindResource("poo_sniffer_xpr") as Brush;

			Cursor = (Target == _pooSniffer) ? null : Cursors.SizeAll;

			UIElement element = this.CreateIfPossible(Target);
			if (element != null)
				this.Viewbox.Child = element;
		}
		#endregion


		protected override bool HandlesScrolling
		{
			get
			{
				return base.HandlesScrolling;
			}
		}


		private void Content_MouseDown(object sender, MouseButtonEventArgs e)
		{
			this.Focus();
			this.downPoint = e.GetPosition(this.DocumentRoot);
			this.DocumentRoot.CaptureMouse();
		}
		private void Content_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsValidTarget && this.DocumentRoot.IsMouseCaptured)
			{
				Vector delta = e.GetPosition(this.DocumentRoot) - this.downPoint;
				this.translation.X += delta.X;
				this.translation.Y += delta.Y;

				this.downPoint = e.GetPosition(this.DocumentRoot);
			}
		}
		private void Content_MouseUp(object sender, MouseEventArgs e)
		{
			this.DocumentRoot.ReleaseMouseCapture();
		}

		public void DoMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (IsValidTarget)
			{
				double zoom = Math.Pow(ZoomFactor, e.Delta / 120.0);
				Point offset = e.GetPosition(this.Viewbox);
				this.Zoom(zoom, offset);
			}
		}


		private void ResetZoomAndTranslation()
		{
			//Zoom(0, new Point(-this.translation.X, -this.translation.Y));
			//Zoom(1.0 / zoom.ScaleX, new Point());
			this.zoom.ScaleX = 1.0;
			this.zoom.ScaleY = 1.0;

			this.translation.X = 0.0;
			this.translation.Y = 0.0;
		}

        private UIElement CreateIfPossible(object item)
        {
            return ZoomerUtilities.CreateIfPossible(item);
        }
        //private UIElement CreateIfPossible(object item)
        //{
        //    if (item is Window && VisualTreeHelper.GetChildrenCount((Visual)item) == 1)
        //        item = VisualTreeHelper.GetChild((Visual)item, 0);

        //    if (item is FrameworkElement)
        //    {
        //        FrameworkElement uiElement = (FrameworkElement)item;
        //        VisualBrush brush = new VisualBrush(uiElement);
        //        brush.Stretch = Stretch.Uniform;
        //        Rectangle rect = new Rectangle();
        //        rect.Fill = brush;
        //        if (uiElement.ActualHeight == 0 && uiElement.ActualWidth == 0)//sometimes the actual size might be 0 despite there being a rendered visual with a size greater than 0. This happens often on a custom panel (http://snoopwpf.codeplex.com/workitem/7217). Having a fixed size visual brush remedies the problem.
        //        {
        //            rect.Width = 50;
        //            rect.Height = 50;
        //        }
        //        else
        //        {
        //            rect.Width = uiElement.ActualWidth;
        //            rect.Height = uiElement.ActualHeight;
        //        }
        //        return rect;
        //    }

        //    else if (item is ResourceDictionary)
        //    {
        //        StackPanel stackPanel = new StackPanel();

        //        foreach (object value in ((ResourceDictionary)item).Values)
        //        {
        //            UIElement element = CreateIfPossible(value);
        //            if (element != null)
        //                stackPanel.Children.Add(element);
        //        }
        //        return stackPanel;
        //    }
        //    else if (item is Brush)
        //    {
        //        Rectangle rect = new Rectangle();
        //        rect.Width = 10;
        //        rect.Height = 10;
        //        rect.Fill = (Brush)item;
        //        return rect;
        //    }
        //    else if (item is ImageSource)
        //    {
        //        Image image = new Image();
        //        image.Source = (ImageSource)item;
        //        return image;
        //    }
        //    return null;
        //}

		private void Zoom(double zoom, Point offset)
		{
			Vector v = new Vector((1 - zoom) * offset.X, (1 - zoom) * offset.Y);

			Vector translationVector = v * this.transform.Value;
			this.translation.X += translationVector.X;
			this.translation.Y += translationVector.Y;

			this.zoom.ScaleX = this.zoom.ScaleX * zoom;
			this.zoom.ScaleY = this.zoom.ScaleY * zoom;
		}


		private bool IsValidTarget
		{
			get
			{
				return Target != null && Target != _pooSniffer;
			}
		}


		private Brush _pooSniffer = null;

		private TranslateTransform translation = new TranslateTransform();
		private ScaleTransform zoom = new ScaleTransform();
		private TransformGroup transform = new TransformGroup();
		private Point downPoint;

		private const double ZoomFactor = 1.1;
	}
}
