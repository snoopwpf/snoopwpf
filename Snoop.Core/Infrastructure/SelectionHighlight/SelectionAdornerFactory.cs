namespace Snoop.Infrastructure.SelectionHighlight
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using Snoop.Controls;

    public static class SelectionAdornerFactory
    {
        public static Adorner? CreateAndAttachAdornerContainer(UIElement uiElement)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);

            if (adornerLayer is null)
            {
                return null;
            }

            var container = CreateAdornerContainer(uiElement, adornerLayer);

            adornerLayer.Add(container);
            return container;
        }

        public static Adorner CreateAdornerContainer(UIElement uiElement, AdornerLayer? adornerLayer)
        {
            return new SelectionAdorner(uiElement)
            {
                AdornerLayer = adornerLayer
            };

            // var border = new Border
            // {
            //     IsHitTestVisible = false,
            //     BorderThickness = new Thickness(SelectionHighlightOptions.Current.Thickness)
            // };
            // //border.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(SelectionHighlightOptions.BorderThickness)) { Source = SelectionHighlightOptions.Current });
            // border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(SelectionHighlightOptions.BorderBrush)) { Source = SelectionHighlightOptions.Current });
            //
            // return new AdornerContainer(uiElement)
            // {
            //     Child = border,
            //     AdornerLayer = adornerLayer
            // };
        }
    }
}