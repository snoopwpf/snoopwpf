namespace Snoop
{
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media;

	public partial class Previewer
	{
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(object), typeof(Previewer));
		public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(Previewer), new PropertyMetadata(false));
		public static readonly DependencyProperty BrushProperty;
		private static readonly DependencyPropertyKey BrushPropertyKey = DependencyProperty.RegisterReadOnly("Brush", typeof(Brush), typeof(Previewer), new PropertyMetadata(null));

		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand("Magnify", typeof(SnoopUI));


		static Previewer() {
			Previewer.BrushProperty = Previewer.BrushPropertyKey.DependencyProperty;
		}

		public Previewer()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
			this.CommandBindings.Add(new CommandBinding(Previewer.MagnifyCommand, this.HandleMagnify, this.HandleCanMagnify));
		}

		protected override void OnInitialized(System.EventArgs e)
		{
			base.OnInitialized(e);

			Brush pooSniffer = (Brush)this.FindResource("poo_sniffer_xpr");
			this.SetValue(Previewer.BrushPropertyKey, pooSniffer);
		}

		public object Target {
			get { return this.GetValue(Previewer.TargetProperty); }
			set { this.SetValue(Previewer.TargetProperty, value); }
		}

		public Brush Brush {
			get { return (Brush)this.GetValue(Previewer.BrushProperty); }
		}

		public bool IsActive {
			get { return (bool)this.GetValue(Previewer.IsActiveProperty); }
			set { this.SetValue(Previewer.IsActiveProperty, value); }
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);

			if (e.Property == Previewer.TargetProperty && this.IsActive) {
				Visual visual = this.Target as Visual;

				if (visual != null && !(visual is Window)) {
					VisualBrush brush = new VisualBrush(visual);
					brush.Stretch = Stretch.Uniform;
					this.SetValue(Previewer.BrushPropertyKey, brush);
				}
			}
			else if (e.Property == Previewer.IsActiveProperty) {
				if (this.IsActive) {
					Visual visual = this.Target as Visual;

					if (visual != null) {
						VisualBrush brush = new VisualBrush(visual);
						brush.Stretch = Stretch.Uniform;
						this.SetValue(Previewer.BrushPropertyKey, brush);
					}
				}
				else {
					Brush pooSniffer = (Brush)this.FindResource("poo_sniffer_xpr");
					this.SetValue(Previewer.BrushPropertyKey, pooSniffer);
				}
			}
		}

		private void HandleCanMagnify(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = this.Target != null;// && !(this.CurrentSelection.Visual is Window);
			e.Handled = true;
		}

		private void HandleMagnify(object sender, ExecutedRoutedEventArgs e) {
			Zoomer zoomer = new Zoomer();
			zoomer.Target = this.Target;
			zoomer.Owner = Application.Current.MainWindow;
			zoomer.Show();
			e.Handled = true;
		}
	}
}
