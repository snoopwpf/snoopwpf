// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Forms.Integration;
using Snoop.Infrastructure;

namespace Snoop
{
	public partial class Zoomer
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

			this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;

			this.InitializeComponent();

			this.transform.Children.Add(this.zoom);
			this.transform.Children.Add(this.translation);

			this.Viewbox.RenderTransform = this.transform;
		}

		public static void GoBabyGo()
		{
			Dispatcher dispatcher;
			if (Application.Current == null && !SnoopModes.MultipleDispatcherMode)
				dispatcher = Dispatcher.CurrentDispatcher;
			else
				dispatcher = Application.Current.Dispatcher;

			if (dispatcher.CheckAccess())
			{
				Zoomer zoomer = new Zoomer();
				zoomer.Magnify();
			}
			else
			{
				dispatcher.Invoke((Action)GoBabyGo);
			}
		}

		public void Magnify()
		{
			object root = FindRoot();
			if (root == null)
			{
				MessageBox.Show
				(
					"Can't find a current application or a PresentationSource root visual!",
					"Can't Magnify",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation
				);
			}

			Magnify(root);
		}

		public void Magnify(object root)
		{
			this.Target = root;

			Window ownerWindow = SnoopWindowUtils.FindOwnerWindow();
			if (ownerWindow != null)
				this.Owner = ownerWindow;

			SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);

			this.Show();
			this.Activate();
		}

		public object Target
		{
			get { return this.target; }
			set
			{
				this.target = value;
				UIElement element = this.CreateIfPossible(value);
				if (element != null)
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

			try
			{
				// load the window placement details from the user settings.
				WINDOWPLACEMENT wp = (WINDOWPLACEMENT)Properties.Settings.Default.ZoomerWindowPlacement;
				wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				wp.flags = 0;
				wp.showCmd = (wp.showCmd == Win32.SW_SHOWMINIMIZED ? Win32.SW_SHOWNORMAL : wp.showCmd);
				IntPtr hwnd = new WindowInteropHelper(this).Handle;
				Win32.SetWindowPlacement(hwnd, ref wp);
			}
			catch
			{
			}
		}

		

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			this.Viewbox.Child = null;

			// persist the window placement details to the user settings.
			WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
			IntPtr hwnd = new WindowInteropHelper(this).Handle;
			Win32.GetWindowPlacement(hwnd, out wp);
			Properties.Settings.Default.ZoomerWindowPlacement = wp;
			Properties.Settings.Default.Save();

			SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
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

		private object FindRoot()
		{
			object root = null;

			if (SnoopModes.MultipleDispatcherMode)
			{
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
					if
					(
						presentationSource.RootVisual != null &&
						presentationSource.RootVisual is UIElement &&
						((UIElement)presentationSource.RootVisual).Dispatcher.CheckAccess()
					)
					{
						root = presentationSource.RootVisual;
						break;
					}
				}
			}
			else if (Application.Current != null)
			{
				// try to use the application's main window (if visible) as the root
				if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
				{
					root = Application.Current.MainWindow;
				}
				else
				{
					// else search for the first visible window in the list of the application's windows
					foreach (Window window in Application.Current.Windows)
					{
						if (window.Visibility == Visibility.Visible)
						{
							root = window;
							break;
						}
					}
				}
			}
			else
			{
				// if we don't have a current application,
				// then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).

				if (System.Windows.Forms.Application.OpenForms.Count > 0)
				{
					// this is windows forms -> wpf interop

					// call ElementHost.EnableModelessKeyboardInterop
					// to allow the Zoomer window to receive keyboard messages.
					ElementHost.EnableModelessKeyboardInterop(this);
				}
			}

			if (root == null)
			{
				// if we still don't have a root to magnify

				// let's iterate over PresentationSource.CurrentSources,
				// and use the first non-null, visible RootVisual we find as root to inspect.
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
					if
					(
						presentationSource.RootVisual != null &&
						presentationSource.RootVisual is UIElement &&
						((UIElement)presentationSource.RootVisual).Visibility == Visibility.Visible
					)
					{
						root = presentationSource.RootVisual;
						break;
					}
				}
			}

			// if the root is a window, let's magnify the window's content.
			// this is better, as otherwise, you will have window background along with the window's content.
			if (root is Window && ((Window)root).Content != null)
				root = ((Window)root).Content;

			return root;
		}
		private void SetOwnerWindow()
		{
			Window ownerWindow = null;

			if (SnoopModes.MultipleDispatcherMode)
			{
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
					if
					(
						presentationSource.RootVisual is Window &&
						((Window)presentationSource.RootVisual).Dispatcher.CheckAccess()
					)
					{
						ownerWindow = (Window)presentationSource.RootVisual;
						break;
					}
				}
			}
			else if (Application.Current != null)
			{
				if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
				{
					// first: set the owner window as the current application's main window, if visible.
					ownerWindow = Application.Current.MainWindow;
				}
				else
				{
					// second: try and find a visible window in the list of the current application's windows
					foreach (Window window in Application.Current.Windows)
					{
						if (window.Visibility == Visibility.Visible)
						{
							ownerWindow = window;
							break;
						}
					}
				}
			}

			if (ownerWindow == null)
			{
				// third: try and find a visible window in the list of current presentation sources
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
					if
					(
						presentationSource.RootVisual is Window &&
						((Window)presentationSource.RootVisual).Visibility == Visibility.Visible
					)
					{
						ownerWindow = (Window)presentationSource.RootVisual;
						break;
					}
				}
			}

			if (ownerWindow != null)
				this.Owner = ownerWindow;
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
