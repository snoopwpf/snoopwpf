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
		public static readonly RoutedCommand ScreenshotCommand = new RoutedCommand("Screenshot", typeof(SnoopUI));


		static Previewer()
		{
			Previewer.BrushProperty = Previewer.BrushPropertyKey.DependencyProperty;
		}

		public Previewer()
		{
			this.InitializeComponent();

			this.CommandBindings.Add(new CommandBinding(Previewer.MagnifyCommand, this.HandleMagnify, this.HandleCanMagnify));
			this.CommandBindings.Add(new CommandBinding(Previewer.ScreenshotCommand, this.HandleScreenshot, this.HandleCanScreenshot));
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
				typeof(Previewer),
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
			((Previewer)d).OnTargetChanged(e);
		}
		/// <summary>
		/// Provides derived classes an opportunity to handle changes to the Target property.
		/// </summary>
		protected virtual void OnTargetChanged(DependencyPropertyChangedEventArgs e)
		{
			HandleTargetOrIsActiveChanged();
		}
		#endregion

		#region IsActive
		/// <summary>
		/// Gets or sets the IsActive property.
		/// </summary>
		public bool IsActive
		{
			get { return (bool)GetValue(IsActiveProperty); }
			set { SetValue(IsActiveProperty, value); }
		}
		/// <summary>
		/// IsActive Dependency Property
		/// </summary>
		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register
			(
				"IsActive",
				typeof(bool),
				typeof(Previewer),
				new FrameworkPropertyMetadata
				(
					(bool)false,
					new PropertyChangedCallback(OnIsActiveChanged)
				)
			);
		/// <summary>
		/// Handles changes to the IsActive property.
		/// </summary>
		private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Previewer)d).OnIsActiveChanged(e);
		}
		/// <summary>
		/// Provides derived classes an opportunity to handle changes to the IsActive property.
		/// </summary>
		protected virtual void OnIsActiveChanged(DependencyPropertyChangedEventArgs e)
		{
			HandleTargetOrIsActiveChanged();
		}
		#endregion

		#region Brush
		/// <summary>
		/// Gets the Brush property.
		/// </summary>
		public Brush Brush
		{
			get { return (Brush)GetValue(BrushProperty); }
		}
		/// <summary>
		/// Brush Read-Only Dependency Property
		/// </summary>
		private static readonly DependencyPropertyKey BrushPropertyKey =
			DependencyProperty.RegisterReadOnly
			(
				"Brush",
				typeof(Brush),
				typeof(Previewer),
				new FrameworkPropertyMetadata((Brush)null)
			);
		public static readonly DependencyProperty BrushProperty = BrushPropertyKey.DependencyProperty;
		/// <summary>
		/// Provides a secure method for setting the Brush property.
		/// </summary>
		protected void SetBrush(Brush value)
		{
			SetValue(BrushPropertyKey, value);
		}
		#endregion


		protected override void OnInitialized(System.EventArgs e)
		{
			base.OnInitialized(e);

			Brush pooSniffer = (Brush)this.FindResource("previewerSnoopDogDrawingBrush");
			this.SetValue(Previewer.BrushPropertyKey, pooSniffer);
		}


		private void HandleTargetOrIsActiveChanged()
		{
			if (this.IsActive && this.Target is Visual)
			{
				Visual visual = (Visual)this.Target;
				VisualBrush brush = new VisualBrush(visual);
				brush.Stretch = Stretch.Uniform;
				SetBrush(brush);
			}
			else
			{
				Brush pooSniffer = (Brush)this.FindResource("previewerSnoopDogDrawingBrush");
				SetBrush(pooSniffer);
			}
		}


		private void HandleCanMagnify(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (this.Target as Visual) != null;
			e.Handled = true;
		}
		private void HandleMagnify(object sender, ExecutedRoutedEventArgs e)
		{
			Zoomer zoomer = new Zoomer();
			zoomer.Magnify(this.Target);
			e.Handled = true;
		}

		private void HandleCanScreenshot(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (this.Target as Visual) != null;
			e.Handled = true;
		}
		private void HandleScreenshot(object sender, ExecutedRoutedEventArgs e)
		{
			Visual visual = this.Target as Visual;

			ScreenshotDialog dialog = new ScreenshotDialog();
			dialog.DataContext = visual;
			dialog.ShowDialog();
			e.Handled = true;
		}
	}
}
