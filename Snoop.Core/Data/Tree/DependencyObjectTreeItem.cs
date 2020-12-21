// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using JetBrains.Annotations;
    using Snoop.Controls;
    using Snoop.Infrastructure;

    /// <summary>
    /// Main class that represents a visual in the visual tree
    /// </summary>
    public class DependencyObjectTreeItem : ResourceContainerTreeItem
    {
        private static readonly Attribute[] propertyFilterAttributes = { new PropertyFilterAttribute(PropertyFilterOptions.All) };

        private AdornerContainer? adornerContainer;

        public DependencyObjectTreeItem(DependencyObject target, TreeItem? parent, TreeService treeService)
            : base(target, parent, treeService)
        {
            this.DependencyObject = target;
            this.Visual = target as Visual;
        }

        public DependencyObject DependencyObject { get; }

        public Visual? Visual { get; }

        public override bool HasBindingError
        {
            get
            {
                var propertyDescriptors = TypeDescriptor.GetProperties(this.DependencyObject, propertyFilterAttributes);

                foreach (PropertyDescriptor? property in propertyDescriptors)
                {
                    if (property is null)
                    {
                        continue;
                    }

                    var dpd = DependencyPropertyDescriptor.FromProperty(property);
                    if (dpd is null)
                    {
                        continue;
                    }

                    var expression = BindingOperations.GetBindingExpressionBase(this.DependencyObject, dpd.DependencyProperty);
                    if (expression is not null
                        && (expression.HasError || expression.Status != BindingStatus.Active))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override Visual? MainVisual => this.Visual;

        public override Brush TreeBackgroundBrush => Brushes.Transparent;

        public override Brush? VisualBrush
        {
            get
            {
                var brush = VisualCaptureUtil.CreateVisualBrushSafe(this.Visual);
                if (brush is not null)
                {
                    brush.Stretch = Stretch.Uniform;
                }

                return brush;
            }
        }

        protected override ResourceDictionary? ResourceDictionary
        {
            get
            {
                if (this.Target is FrameworkElement frameworkElement)
                {
                    return frameworkElement.Resources;
                }

                if (this.Target is FrameworkContentElement frameworkContentElement)
                {
                    return frameworkContentElement.Resources;
                }

                return null;
            }
        }

        protected override void OnIsSelectedChanged()
        {
            if (this.Visual is null)
            {
                return;
            }

            // Add adorners for the visual this is representing.
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.Visual);

            if (adornerLayer is not null
                && this.Visual is UIElement visualElement)
            {
                if (this.IsSelected
                    && this.adornerContainer is null)
                {
                    var border = new Border
                    {
                        BorderThickness = new Thickness(4),
                        IsHitTestVisible = false
                    };

                    var borderColor = new Color
                    {
                        ScA = .3f,
                        ScR = 1
                    };
                    border.BorderBrush = new SolidColorBrush(borderColor);

                    this.adornerContainer = new AdornerContainer(visualElement)
                    {
                        Child = border
                    };
                    adornerLayer.Add(this.adornerContainer);
                }
                else if (this.adornerContainer is not null)
                {
                    adornerLayer.Remove(this.adornerContainer);
                    this.adornerContainer.Child = null;
                    this.adornerContainer = null;
                }
            }
        }
        
        protected override void ReloadCore()
        {
            // having the call to base.ReloadCore here ... puts the application resources at the very top of the tree view.
            // this used to be at the bottom. putting it here makes it consistent (and easier to use) with ApplicationTreeItem
            base.ReloadCore();

            foreach (var child in this.TreeService.GetChildren(this))
            {
                if (child is null)
                {
                    continue;
                }

                this.Children.Add(this.TreeService.Construct(child, this));
            }

            if (this.Target is Grid grid
                // The logical tree already contains these elements
                && this.TreeService.TreeType != TreeType.Logical)
            {
                foreach (var row in grid.RowDefinitions)
                {
                    this.Children.Add(this.TreeService.Construct(row, this));
                }

                foreach (var column in grid.ColumnDefinitions)
                {
                    this.Children.Add(this.TreeService.Construct(column, this));
                }
            }
        }
    }
}