// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

public static class ZoomerUtilities
{
    public static UIElement? CreateIfPossible(object? item)
    {
        if (item is Window window
            && VisualTreeHelper.GetChildrenCount(window) == 1)
        {
            item = VisualTreeHelper.GetChild(window, 0);
        }

        switch (item)
        {
            case FrameworkElement element:
            {
                return CreateRectangleForFrameworkElement(element);
            }

            case Visual visual:
            {
                return CreateRectangleForVisual(visual);
            }

            case ResourceDictionary resourceDictionary:
            {
                var stackPanel = new StackPanel();

                foreach (var value in resourceDictionary.Values)
                {
                    var element = CreateIfPossible(value);
                    if (element is not null)
                    {
                        stackPanel.Children.Add(element);
                    }
                }

                return stackPanel;
            }

            case Brush brush:
            {
                var rect = new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    Fill = brush
                };
                return rect;
            }

            case ImageSource imageSource:
            {
                var image = new Image
                {
                    Source = imageSource
                };
                return image;
            }
        }

        return null;
    }

    private static UIElement CreateRectangleForVisual(Visual uiElement)
    {
        var brush = new VisualBrush(uiElement)
        {
            Stretch = Stretch.Uniform
        };

        var rect = new Rectangle
        {
            Fill = brush,
            Width = 50,
            Height = 50
        };

        return rect;
    }

    private static UIElement? CreateRectangleForFrameworkElement(FrameworkElement uiElement)
    {
        var brush = VisualCaptureUtil.CreateVisualBrushSafe(uiElement);
        if (brush is null)
        {
            return null;
        }

        brush.Stretch = Stretch.Uniform;

        var rect = new Rectangle
        {
            Fill = brush
        };

        // Sometimes the actual size might be 0 despite there being a rendered visual with a size greater than 0.
        // This happens often on a custom panel (http://snoopwpf.codeplex.com/workitem/7217).
        // Having a fixed size visual brush remedies the problem.
        if (uiElement.ActualHeight == 0
            && uiElement.ActualWidth == 0)
        {
            rect.Width = 50;
            rect.Height = 50;
        }
        else
        {
            rect.Width = uiElement.ActualWidth;
            rect.Height = uiElement.ActualHeight;
        }

        return rect;
    }
}