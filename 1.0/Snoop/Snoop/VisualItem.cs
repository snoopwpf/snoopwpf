namespace Snoop
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Documents;
	using System.Windows.Controls;
	using System.Windows.Data;

	/// <summary>
	/// Main class that represents a visual in the visual tree
	/// </summary>
	public class VisualItem : VisualTreeItem
	{
		private Visual visual;
		private AdornerContainer adorner;

		public VisualItem(Visual visual, VisualTreeItem parent): base(visual, parent) {
			this.visual = visual;
		}

		public Visual Visual {
			get { return this.visual; }
		}


		protected override void OnSelectionChanged() {
			// Add adorners for the visual this is representing.
			AdornerLayer adorners = AdornerLayer.GetAdornerLayer(this.Visual);
			UIElement visualElement = this.Visual as UIElement;

			if (adorners != null && visualElement != null) {
				if (this.IsSelected && this.adorner == null) {
					Border border = new Border();
					border.BorderThickness = new Thickness(4);

					Color borderColor = new Color();
					borderColor.ScA = .3f;
					borderColor.ScR = 1;
					border.BorderBrush = new SolidColorBrush(borderColor);

					border.IsHitTestVisible = false;
					this.adorner = new AdornerContainer(visualElement);
					adorner.Child = border;
					adorners.Add(adorner);
				}
				else if (this.adorner != null) {
					adorners.Remove(this.adorner);
					this.adorner.Child = null;
					this.adorner = null;
				}
			}
		}
	}
}
