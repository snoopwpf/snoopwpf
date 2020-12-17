// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Snoop.Infrastructure;

    /// <summary>
    /// Interaction logic for ZoomerControl.xaml
    /// </summary>
    public partial class ZoomerControl : UserControl
    {
        public ZoomerControl()
        {
            this.InitializeComponent();

            this.transform.Children.Add(this.zoom);
            this.transform.Children.Add(this.translation);

            this.Viewbox.RenderTransform = this.transform;
        }

        #region Target

        /// <summary>
        /// Gets or sets the Target property.
        /// </summary>
        public object Target
        {
            get { return (object)this.GetValue(TargetProperty); }
            set { this.SetValue(TargetProperty, value); }
        }

        /// <summary>
        /// Target Dependency Property
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                nameof(Target),
                typeof(object),
                typeof(ZoomerControl),
                new FrameworkPropertyMetadata(
                    default,
                    OnTargetChanged));

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
            this.ResetZoomAndTranslation();

            if (this.pooSniffer == null)
            {
                this.pooSniffer = this.TryFindResource("poo_sniffer_xpr") as Brush;
            }

            this.Cursor = this.Target == this.pooSniffer ? null : Cursors.SizeAll;

            var element = this.CreateIfPossible(this.Target);
            if (element != null)
            {
                this.Viewbox.Child = element;
            }
        }
        #endregion

        private void Content_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
            this.downPoint = e.GetPosition(this.DocumentRoot);
            this.DocumentRoot.CaptureMouse();
        }

        private void Content_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.IsValidTarget && this.DocumentRoot.IsMouseCaptured)
            {
                var delta = e.GetPosition(this.DocumentRoot) - this.downPoint;
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
            if (this.IsValidTarget)
            {
                var zoom = Math.Pow(ZoomFactor, e.Delta / 120.0);
                var offset = e.GetPosition(this.Viewbox);
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

        private UIElement? CreateIfPossible(object? item)
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
            var v = new Vector((1 - zoom) * offset.X, (1 - zoom) * offset.Y);

            var translationVector = v * this.transform.Value;
            this.translation.X += translationVector.X;
            this.translation.Y += translationVector.Y;

            this.zoom.ScaleX *= zoom;
            this.zoom.ScaleY *= zoom;
        }

        private bool IsValidTarget
        {
            get
            {
                return this.Target != null && this.Target != this.pooSniffer;
            }
        }

        private Brush? pooSniffer;

        private readonly TranslateTransform translation = new TranslateTransform();
        private readonly ScaleTransform zoom = new ScaleTransform();
        private readonly TransformGroup transform = new TransformGroup();
        private Point downPoint;

        private const double ZoomFactor = 1.1;
    }
}
