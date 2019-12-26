// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Snoop.Infrastructure;

    public sealed partial class Zoomer
    {
        static Zoomer()
        {
            Zoomer.ResetCommand = new RoutedCommand("Reset", typeof(Zoomer));
            Zoomer.ZoomInCommand = new RoutedCommand("ZoomIn", typeof(Zoomer));
            Zoomer.ZoomOutCommand = new RoutedCommand("ZoomOut", typeof(Zoomer));
            Zoomer.PanLeftCommand = new RoutedCommand("PanLeft", typeof(Zoomer));
            Zoomer.PanRightCommand = new RoutedCommand("PanRight", typeof(Zoomer));
            Zoomer.PanUpCommand = new RoutedCommand("PanUp", typeof(Zoomer));
            Zoomer.PanDownCommand = new RoutedCommand("PanDown", typeof(Zoomer));
            Zoomer.SwitchTo2DCommand = new RoutedCommand("SwitchTo2D", typeof(Zoomer));
            Zoomer.SwitchTo3DCommand = new RoutedCommand("SwitchTo3D", typeof(Zoomer));

            Zoomer.ResetCommand.InputGestures.Add(new MouseGesture(MouseAction.LeftDoubleClick));
            Zoomer.ResetCommand.InputGestures.Add(new KeyGesture(Key.F5));
            Zoomer.ZoomInCommand.InputGestures.Add(new KeyGesture(Key.OemPlus));
            Zoomer.ZoomInCommand.InputGestures.Add(new KeyGesture(Key.Up, ModifierKeys.Control));
            Zoomer.ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.OemMinus));
            Zoomer.ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Control));
            Zoomer.PanLeftCommand.InputGestures.Add(new KeyGesture(Key.Left));
            Zoomer.PanRightCommand.InputGestures.Add(new KeyGesture(Key.Right));
            Zoomer.PanUpCommand.InputGestures.Add(new KeyGesture(Key.Up));
            Zoomer.PanDownCommand.InputGestures.Add(new KeyGesture(Key.Down));
            Zoomer.SwitchTo2DCommand.InputGestures.Add(new KeyGesture(Key.F2));
            Zoomer.SwitchTo3DCommand.InputGestures.Add(new KeyGesture(Key.F3));
        }

        public Zoomer()
        {
            this.CommandBindings.Add(new CommandBinding(Zoomer.ResetCommand, this.HandleReset, this.CanReset));
            this.CommandBindings.Add(new CommandBinding(Zoomer.ZoomInCommand, this.HandleZoomIn));
            this.CommandBindings.Add(new CommandBinding(Zoomer.ZoomOutCommand, this.HandleZoomOut));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanLeftCommand, this.HandlePanLeft));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanRightCommand, this.HandlePanRight));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanUpCommand, this.HandlePanUp));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanDownCommand, this.HandlePanDown));
            this.CommandBindings.Add(new CommandBinding(Zoomer.SwitchTo2DCommand, this.HandleSwitchTo2D));
            this.CommandBindings.Add(new CommandBinding(Zoomer.SwitchTo3DCommand, this.HandleSwitchTo3D, this.CanSwitchTo3D));

            this.InitializeComponent();

            this.transform.Children.Add(this.zoom);
            this.transform.Children.Add(this.translation);

            this.Viewbox.RenderTransform = this.transform;
        }

        protected override void Load(object root)
        {
            this.Target = root;
        }

        public override object Target
        {
            get { return this.target; }

            set
            {
                this.target = value;
                var element = this.CreateIfPossible(value);
                this.Viewbox.Child = element;
            }
        }

        public static readonly RoutedCommand ResetCommand;
        public static readonly RoutedCommand ZoomInCommand;
        public static readonly RoutedCommand ZoomOutCommand;
        public static readonly RoutedCommand PanLeftCommand;
        public static readonly RoutedCommand PanRightCommand;
        public static readonly RoutedCommand PanUpCommand;
        public static readonly RoutedCommand PanDownCommand;
        public static readonly RoutedCommand SwitchTo2DCommand;
        public static readonly RoutedCommand SwitchTo3DCommand;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // load the window placement details from the user settings.
            SnoopWindowUtils.LoadWindowPlacement(this, Properties.Settings.Default.ZoomerWindowPlacement);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            this.Viewbox.Child = null;

            // persist the window placement details to the user settings.
            SnoopWindowUtils.SaveWindowPlacement(this, wp => Properties.Settings.Default.ZoomerWindowPlacement = wp);
        }

        /// <inheritdoc />
        protected override object FindRoot()
        {
            var root = base.FindRoot();

            if (root is Application application)
            {
                // try to use the application's main window (if visible) as the root
                if (application.MainWindow != null
                    && application.MainWindow.Visibility == Visibility.Visible)
                {
                    root = application.MainWindow;
                }
                else
                {
                    // else search for the first visible window in the list of the application's windows
                    foreach (Window appWindow in application.Windows)
                    {
                        if (appWindow.CheckAccess()
                            && appWindow.Visibility == Visibility.Visible)
                        {
                            root = appWindow;
                            break;
                        }
                    }
                }
            }

            return root;
        }

        private void HandleReset(object target, ExecutedRoutedEventArgs args)
        {
            this.translation.X = 0;
            this.translation.Y = 0;
            this.zoom.ScaleX = 1;
            this.zoom.ScaleY = 1;
            this.zoom.CenterX = 0;
            this.zoom.CenterY = 0;

            if (this.visualTree3DView != null)
            {
                this.visualTree3DView.Reset();
                this.ZScaleSlider.Value = 0;
            }
        }

        private void CanReset(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
            args.Handled = true;
        }

        private void HandleZoomIn(object target, ExecutedRoutedEventArgs args)
        {
            Point offset = Mouse.GetPosition(this.Viewbox);
            this.Zoom(Zoomer.ZoomFactor, offset);
        }

        private void HandleZoomOut(object target, ExecutedRoutedEventArgs args)
        {
            Point offset = Mouse.GetPosition(this.Viewbox);
            this.Zoom(1 / Zoomer.ZoomFactor, offset);
        }

        private void HandlePanLeft(object target, ExecutedRoutedEventArgs args)
        {
            this.translation.X -= 5;
        }

        private void HandlePanRight(object target, ExecutedRoutedEventArgs args)
        {
            this.translation.X += 5;
        }

        private void HandlePanUp(object target, ExecutedRoutedEventArgs args)
        {
            this.translation.Y -= 5;
        }

        private void HandlePanDown(object target, ExecutedRoutedEventArgs args)
        {
            this.translation.Y += 5;
        }

        private void HandleSwitchTo2D(object target, ExecutedRoutedEventArgs args)
        {
            if (this.visualTree3DView != null)
            {
                this.Target = this.target;
                this.visualTree3DView = null;
                this.ZScaleSlider.Visibility = Visibility.Collapsed;
            }
        }

        private void HandleSwitchTo3D(object target, ExecutedRoutedEventArgs args)
        {
            Visual visual = this.target as Visual;
            if (this.visualTree3DView == null && visual != null)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    this.visualTree3DView = new VisualTree3DView(visual);
                    this.Viewbox.Child = this.visualTree3DView;
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }

                this.ZScaleSlider.Visibility = Visibility.Visible;
            }
        }

        private void CanSwitchTo3D(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = (this.target is Visual);
            args.Handled = true;
        }

        private void Content_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.downPoint = e.GetPosition(this.DocumentRoot);
            this.DocumentRoot.CaptureMouse();
        }

        private void Content_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.DocumentRoot.IsMouseCaptured)
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

        private void Content_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = Math.Pow(Zoomer.ZoomFactor, e.Delta / 120.0);
            Point offset = e.GetPosition(this.Viewbox);
            this.Zoom(zoom, offset);
        }

        private void ZScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.visualTree3DView != null)
            {
                this.visualTree3DView.ZScale = Math.Pow(10, e.NewValue);
            }
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
        //        rect.Width = uiElement.ActualWidth;
        //        rect.Height = uiElement.ActualHeight;
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

        private TranslateTransform translation = new TranslateTransform();
        private ScaleTransform zoom = new ScaleTransform();
        private TransformGroup transform = new TransformGroup();
        private Point downPoint;
        private object target;
        private VisualTree3DView visualTree3DView;

        private const double ZoomFactor = 1.1;

        private delegate void Action();
    }

    public class DoubleToWhitenessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float val = (float)(double)value;
            Color c = new Color();
            c.ScR = val;
            c.ScG = val;
            c.ScB = val;
            c.ScA = 1;

            return new SolidColorBrush(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
