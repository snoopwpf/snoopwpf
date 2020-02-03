// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using JetBrains.Annotations;
    using Snoop.Infrastructure;

    /// <summary>
    /// Main class that represents a visual in the visual tree
    /// </summary>
    public class VisualTreeItem : ResourceContainerTreeItem
    {
        private static readonly Attribute[] propertyFilterAttributes = { new PropertyFilterAttribute(PropertyFilterOptions.All) };

        private AdornerContainer adornerContainer;
        
        public VisualTreeItem(DependencyObject target, TreeItem parent)
            : base(target, parent)
        {
            this.DependencyObject = target;
            this.Visual = target as Visual;
        }

        [NotNull]
        public DependencyObject DependencyObject { get; }

        [CanBeNull]
        public Visual Visual { get; }

        public override bool HasBindingError
        {
            get
            {
                var propertyDescriptors = TypeDescriptor.GetProperties(this.DependencyObject, propertyFilterAttributes);

                foreach (PropertyDescriptor property in propertyDescriptors)
                {
                    var dpd = DependencyPropertyDescriptor.FromProperty(property);
                    if (dpd == null)
                    {
                        continue;
                    }

                    var expression = BindingOperations.GetBindingExpressionBase(this.DependencyObject, dpd.DependencyProperty);
                    if (expression != null
                        && (expression.HasError || expression.Status != BindingStatus.Active))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override Visual MainVisual => this.Visual;

        public override Brush TreeBackgroundBrush => Brushes.Transparent;

        public override Brush VisualBrush
        {
            get
            {
                var brush = VisualCaptureUtil.CreateVisualBrushSafe(this.Visual);
                if (brush != null)
                {
                    brush.Stretch = Stretch.Uniform;
                }

                return brush;
            }
        }

        protected override ResourceDictionary ResourceDictionary
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

            if (adornerLayer != null
                && this.Visual is UIElement visualElement)
            {
                if (this.IsSelected
                    && this.adornerContainer == null)
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
                else if (this.adornerContainer != null)
                {
                    adornerLayer.Remove(this.adornerContainer);
                    this.adornerContainer.Child = null;
                    this.adornerContainer = null;
                }
            }
        }

        protected override void Reload(List<TreeItem> toBeRemoved)
        {
            // having the call to base.Reload here ... puts the application resources at the very top of the tree view.
            // this used to be at the bottom. putting it here makes it consistent (and easier to use) with ApplicationTreeItem
            base.Reload(toBeRemoved);

            if (this.Target is Window window)
            {
                foreach (Window ownedWindow in window.OwnedWindows)
                {
                    if (ownedWindow.IsInitialized == false
                        || ownedWindow.CheckAccess() == false
                        || ownedWindow.IsPartOfSnoopVisualTree())
                    {
                        continue;
                    }

                    // don't recreate existing items but reload them instead
                    var existingItem = toBeRemoved.FirstOrDefault(x => ReferenceEquals(x.Target, ownedWindow));
                    if (existingItem != null)
                    {
                        toBeRemoved.Remove(existingItem);
                        existingItem.Reload();
                        continue;
                    }

                    this.Children.Add(Construct(ownedWindow, this));
                }
            }

            // remove items that are no longer in tree, add new ones.
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(this.DependencyObject); i++)
            {
                var child = VisualTreeHelper.GetChild(this.DependencyObject, i);

                // ReSharper disable HeuristicUnreachableCode
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (child is null)
                {
                    continue;
                }

                // ReSharper restore HeuristicUnreachableCode

                var foundItem = false;
                foreach (var item in toBeRemoved)
                {
                    if (ReferenceEquals(item.Target, child))
                    {
                        toBeRemoved.Remove(item);
                        item.Reload();
                        foundItem = true;
                        break;
                    }
                }

                if (foundItem == false)
                {
                    this.Children.Add(Construct(child, this));
                }
            }

            if (this.Visual is Grid grid)
            {
                foreach (var row in grid.RowDefinitions)
                {
                    this.Children.Add(Construct(row, this));
                }

                foreach (var column in grid.ColumnDefinitions)
                {
                    this.Children.Add(Construct(column, this));
                }
            }
        }
    }
}