// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace Snoop.Infrastructure
{
    public static class ZoomerUtilities
    {
        public static UIElement CreateIfPossible(object item)
        {
            if (item is Window && VisualTreeHelper.GetChildrenCount((Visual)item) == 1)
                item = VisualTreeHelper.GetChild((Visual)item, 0);

            if (item is FrameworkElement)
            {
                FrameworkElement uiElement = (FrameworkElement)item;
                return CreateRectangleForFrameworkElement(uiElement);
            }
            else if (item is Visual)
            {
                Visual visual = (Visual)item;
                return CreateRectangleForVisual(visual);
            }
            else if (item is ResourceDictionary)
            {
                StackPanel stackPanel = new StackPanel();

                foreach (object value in ((ResourceDictionary)item).Values)
                {
                    UIElement element = CreateIfPossible(value);
                    if (element != null)
                        stackPanel.Children.Add(element);
                }
                return stackPanel;
            }
            else if (item is Brush)
            {
                Rectangle rect = new Rectangle();
                rect.Width = 10;
                rect.Height = 10;
                rect.Fill = (Brush)item;
                return rect;
            }
            else if (item is ImageSource)
            {
                Image image = new Image();
                image.Source = (ImageSource)item;
                return image;
            }
            return null;
        }

        private static UIElement CreateRectangleForVisual(Visual uiElement)
        {
            VisualBrush brush = new VisualBrush(uiElement);
            brush.Stretch = Stretch.Uniform;
            Rectangle rect = new Rectangle();
            rect.Fill = brush;
            rect.Width = 50;
            rect.Height = 50;

            return rect;
        }

        private static UIElement CreateRectangleForFrameworkElement(FrameworkElement uiElement)
        {
            VisualBrush brush = new VisualBrush(uiElement);
            brush.Stretch = Stretch.Uniform;
            Rectangle rect = new Rectangle();
            rect.Fill = brush;
            if (uiElement.ActualHeight == 0 && uiElement.ActualWidth == 0)//sometimes the actual size might be 0 despite there being a rendered visual with a size greater than 0. This happens often on a custom panel (http://snoopwpf.codeplex.com/workitem/7217). Having a fixed size visual brush remedies the problem.
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
}
