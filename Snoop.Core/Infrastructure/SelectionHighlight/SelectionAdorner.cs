namespace Snoop.Infrastructure.SelectionHighlight
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Snoop.AttachedProperties;
    using Snoop.Infrastructure.Helpers;

    public class SelectionAdorner : Adorner, IDisposable
    {
        static SelectionAdorner()
        {
            IsHitTestVisibleProperty.OverrideMetadata(typeof(SelectionAdorner), new UIPropertyMetadata(false));
            UseLayoutRoundingProperty.OverrideMetadata(typeof(SelectionAdorner), new FrameworkPropertyMetadata(true));
            SnoopAttachedProperties.IsSnoopPartProperty.OverrideMetadata(typeof(SelectionAdorner), new FrameworkPropertyMetadata(true));
        }

        public SelectionAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            SelectionHighlightOptions.Default.PropertyChanged += this.SelectionHighlightOptionsOnPropertyChanged;
        }

        public AdornerLayer? AdornerLayer { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (SelectionHighlightOptions.Default.HighlightSelectedItem == false)
            {
                return;
            }

            if (this.ActualWidth.AreClose(0)
                || this.ActualHeight.AreClose(0))
            {
                return;
            }

            var pen = SelectionHighlightOptions.Default.Pen;
            var thickness = pen.Thickness;

            var width = this.ActualWidth - thickness;
            var height = this.ActualHeight - thickness;

            if (width.GreaterThan(0) == false
                || height.GreaterThan(0) == false)
            {
                return;
            }

            drawingContext.DrawRectangle(SelectionHighlightOptions.Default.Background, pen, new Rect(thickness, thickness, width, height));
        }

        public void Dispose()
        {
            SelectionHighlightOptions.Default.PropertyChanged -= this.SelectionHighlightOptionsOnPropertyChanged;

            this.AdornerLayer?.Remove(this);
        }

        private void SelectionHighlightOptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
        }
    }
}