// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Snoop
{
	public partial class Previewer
	{
		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand("Magnify", typeof(SnoopUI));


		static Previewer()
		{
			Previewer.BrushProperty = Previewer.BrushPropertyKey.DependencyProperty;
		}

		public Previewer()
		{
			this.InitializeComponent();

			this.CommandBindings.Add(new CommandBinding(Previewer.MagnifyCommand, this.HandleMagnify, this.HandleCanMagnify));
		}


		public object Target
		{
			get { return this.GetValue(Previewer.TargetProperty); }
			set { this.SetValue(Previewer.TargetProperty, value); }
		}
		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.Register
			(
				"Target",
				typeof(object),
				typeof(Previewer)
			);

		public Brush Brush
		{
			get { return (Brush)this.GetValue(Previewer.BrushProperty); }
		}
		public static readonly DependencyProperty BrushProperty;
		private static readonly DependencyPropertyKey BrushPropertyKey =
			DependencyProperty.RegisterReadOnly
			(
				"Brush",
				typeof(Brush),
				typeof(Previewer),
				new PropertyMetadata(null)
			);

		public bool IsActive
		{
			get { return (bool)this.GetValue(Previewer.IsActiveProperty); }
			set { this.SetValue(Previewer.IsActiveProperty, value); }
		}
		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register
			(
				"IsActive",
				typeof(bool),
				typeof(Previewer),
				new PropertyMetadata(false)
			);


		protected override void OnInitialized(System.EventArgs e)
		{
			base.OnInitialized(e);

			Brush pooSniffer = (Brush)this.FindResource("poo_sniffer_xpr");
			this.SetValue(Previewer.BrushPropertyKey, pooSniffer);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == Previewer.TargetProperty && this.IsActive)
			{
				Visual visual = this.Target as Visual;

				if (visual != null && !(visual is Window))
				{
					VisualBrush brush = new VisualBrush(visual);
					brush.Stretch = Stretch.Uniform;
					this.SetValue(Previewer.BrushPropertyKey, brush);
				}
			}
			else if (e.Property == Previewer.IsActiveProperty)
			{
				if (this.IsActive)
				{
					Visual visual = this.Target as Visual;

					if (visual != null)
					{
						VisualBrush brush = new VisualBrush(visual);
						brush.Stretch = Stretch.Uniform;
						this.SetValue(Previewer.BrushPropertyKey, brush);
					}
				}
				else
				{
					Brush pooSniffer = (Brush)this.FindResource("poo_sniffer_xpr");
					this.SetValue(Previewer.BrushPropertyKey, pooSniffer);
				}
			}
		}


		private void HandleCanMagnify(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = this.Target != null;
			e.Handled = true;
		}
		private void HandleMagnify(object sender, ExecutedRoutedEventArgs e)
		{
			Zoomer zoomer = new Zoomer();
			zoomer.Magnify(this.Target);
			e.Handled = true;
		}
	}
}
