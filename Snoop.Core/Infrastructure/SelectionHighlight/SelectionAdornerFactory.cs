namespace Snoop.Infrastructure.SelectionHighlight
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using Snoop.Controls;

    public static class SelectionAdornerFactory
    {
        public static AdornerContainer? CreateAndAttachAdornerContainer(UIElement uiElement)
        {
            if (SelectionHighlightOptions.Current.HighlightSelectedItem == false)
            {
                return null;
            }

            var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);

            if (adornerLayer is null)
            {
                return null;
            }

            var container = CreateAdornerContainer(uiElement);
            container.AdornerLayer = adornerLayer;
            adornerLayer.Add(container);
            return container;
        }

        public static AdornerContainer CreateAdornerContainer(UIElement uiElement)
        {
            var border = new Border
            {
                IsHitTestVisible = false
            };
            border.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(SelectionHighlightOptions.BorderThickness)) { Source = SelectionHighlightOptions.Current });
            border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(SelectionHighlightOptions.BorderBrush)) { Source = SelectionHighlightOptions.Current });

            return new AdornerContainer(uiElement)
            {
                Child = border
            };
        }
    }
}