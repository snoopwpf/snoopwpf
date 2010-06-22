namespace Snoop {
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Shapes;
	using System.Windows.Data;
	using System.Globalization;


	public partial class Zoomer {

		private TranslateTransform translation = new TranslateTransform();
		private ScaleTransform zoom = new ScaleTransform();
		private TransformGroup transform = new TransformGroup();

		private Point downPoint;

		public static RoutedCommand ResetCommand;
		private object target;

		static Zoomer() {
			Zoomer.ResetCommand = new RoutedCommand("Reset", typeof(Zoomer));
			Zoomer.ResetCommand.InputGestures.Add(new MouseGesture(MouseAction.LeftDoubleClick));
			Zoomer.ResetCommand.InputGestures.Add(new KeyGesture(Key.F5));
		}

		public Zoomer() {
			this.CommandBindings.Add(new CommandBinding(Zoomer.ResetCommand, this.OnResetCommand, this.CanReset));

			this.InitializeComponent();

			this.transform.Children.Add(this.zoom);
			this.transform.Children.Add(this.translation);

			this.Viewbox.RenderTransform = this.transform;
		}

		public static void GoBabyGo()
		{
			if (Application.Current != null && Application.Current.MainWindow != null) {
				Zoomer zoomer = new Zoomer();
				zoomer.Target = Application.Current.MainWindow.Content;
				zoomer.Show();
			}
		}

		public object Target {
			get { return this.target; }
			set {
				this.target = value;
				UIElement element = this.CreateIfPossible(value);
				if (element != null)
					this.Viewbox.Child = element;
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);
			this.Viewbox.Child = null;
		}

		private void OnResetCommand(object target, ExecutedRoutedEventArgs args) {
			this.translation.X = 0;
			this.translation.Y = 0;
			this.zoom.ScaleX = 1;
			this.zoom.ScaleY = 1;
			this.zoom.CenterX = 0;
			this.zoom.CenterY = 0;
		}

		private void CanReset(object target, CanExecuteRoutedEventArgs args) {
			args.CanExecute = true;
			args.Handled = true;
		}

		private UIElement CreateIfPossible(object item) {
			if (item is Window && VisualTreeHelper.GetChildrenCount((Visual)item) == 1)
				item = VisualTreeHelper.GetChild((Visual)item, 0);

			if (item is FrameworkElement) {
				FrameworkElement uiElement = (FrameworkElement)item;
				VisualBrush brush = new VisualBrush(uiElement);
				brush.Stretch = Stretch.Uniform;
				Rectangle rect = new Rectangle();
				rect.Fill = brush;
				rect.Width = uiElement.ActualWidth;
				rect.Height = uiElement.ActualHeight;
				return rect;
			}

			else if (item is ResourceDictionary) {
				StackPanel stackPanel = new StackPanel();

				foreach (object value in ((ResourceDictionary)item).Values) {
					UIElement element = CreateIfPossible(value);
					if (element != null)
						stackPanel.Children.Add(element);
				}
				return stackPanel;
			}
			else if (item is Brush) {
				Rectangle rect = new Rectangle();
				rect.Width = 10;
				rect.Height = 10;
				rect.Fill = (Brush)item;
				return rect;
			}
			else if (item is ImageSource) {
				Image image = new Image();
				image.Source = (ImageSource)item;
				return image;
			}
			return null;
		}

		void Content_MouseDown(object sender, MouseButtonEventArgs e) {
			this.downPoint = e.GetPosition(this.DocumentRoot);
			this.DocumentRoot.CaptureMouse();
		}

		void Content_MouseMove(object sender, MouseEventArgs e) {
			if (this.DocumentRoot.IsMouseCaptured) {
				Vector delta = e.GetPosition(this.DocumentRoot) - this.downPoint;
				this.translation.X += delta.X;
				this.translation.Y += delta.Y;

				this.downPoint = e.GetPosition(this.DocumentRoot);
			}
		}

		void Content_MouseUp(object sender, MouseEventArgs e) {
			this.DocumentRoot.ReleaseMouseCapture();
		}

		void Content_MouseWheel(object sender, MouseWheelEventArgs e) {
			double zoom = Math.Abs(e.Delta) / 120 * 1.3;
			if (e.Delta < 0)
				zoom = 1 / zoom;

			Point offset = e.GetPosition(this.Viewbox);

			Vector v = new Vector((1 - zoom) * offset.X, (1 - zoom) * offset.Y);

			Vector translationVector = v * this.transform.Value;
			this.translation.X += translationVector.X;
			this.translation.Y += translationVector.Y;

			this.zoom.ScaleX = this.zoom.ScaleX * zoom;
			this.zoom.ScaleY = this.zoom.ScaleY * zoom;
		}
	}

	public class DoubleToWhitenessConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			float val = (float)(double)value;
			Color c = new Color();
			c.ScR = val;
			c.ScG = val;
			c.ScB = val;
			c.ScA = 1;

			return new SolidColorBrush(c);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return null;
		}
	}
}
