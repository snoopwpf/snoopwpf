// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Diagnostics;
using Snoop.Infrastructure.Diagnostics.Providers;
using Snoop.Infrastructure.Helpers;
using Snoop.Infrastructure.SelectionHighlight;

/// <summary>
/// Main class that represents a visual in the visual tree
/// </summary>
public class DependencyObjectTreeItem : ResourceContainerTreeItem
{
    private static readonly Attribute[] propertyFilterAttributes = { new PropertyFilterAttribute(PropertyFilterOptions.All) };

    private IDisposable? selectionHighlight;
    private DiagnosticItem? missingAdornerLayerDiagnosticItem;

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

    protected override IEnumerable<ResourceDictionaryWrapper?> ResourceDictionary
    {
        get
        {
            switch (this.Target)
            {
                case FrameworkElement fe:
                    yield return fe.Resources;

                {
                    var resources = FrameworkElementHelper.GetStyle(fe)?.Resources;
                    if (resources is not null)
                    {
                        yield return new ResourceDictionaryWrapper(resources, "style");
                    }
                }

                {
                    var resources = FrameworkElementHelper.GetTemplate(fe)?.Resources;
                    if (resources is not null)
                    {
                        yield return new ResourceDictionaryWrapper(resources, "template");
                    }
                }

                {
                    var resources = FrameworkElementHelper.GetThemeStyle(fe)?.Resources;
                    if (resources is not null)
                    {
                        yield return new ResourceDictionaryWrapper(resources, "theme style");
                    }
                }

                    break;

                case FrameworkContentElement fce:
                    yield return fce.Resources;

                {
                    var resources = FrameworkElementHelper.GetStyle(fce)?.Resources;
                    if (resources is not null)
                    {
                        yield return new ResourceDictionaryWrapper(resources, "style");
                    }
                }

                {
                    var resources = FrameworkElementHelper.GetThemeStyle(fce)?.Resources;
                    if (resources is not null)
                    {
                        yield return new ResourceDictionaryWrapper(resources, "theme style");
                    }
                }

                    break;
            }
        }
    }

    protected override void OnIsSelectedChanged()
    {
        // Add adorner for the visual this is representing.
        if (this.Target is DependencyObject dependencyObject)
        {
            if (this.IsSelected
                && this.selectionHighlight is null)
            {
                this.selectionHighlight = SelectionHighlightFactory.CreateAndAttachSelectionHighlight(dependencyObject);

                if (this.selectionHighlight is null
                    && this.missingAdornerLayerDiagnosticItem is null
                    && this.Target is not Window)
                {
                    this.missingAdornerLayerDiagnosticItem = new DiagnosticItem(MissingAdornerLayerDiagnosticProvider.Instance, area: DiagnosticArea.Highlight, level: DiagnosticLevel.Critical)
                    {
                        TreeItem = this,
                        Dispatcher = dependencyObject.Dispatcher,
                        SourceObject = dependencyObject
                    };
                    this.TreeService.DiagnosticContext.DiagnosticItems.Add(this.missingAdornerLayerDiagnosticItem);
                }
            }
            else
            {
                if (this.missingAdornerLayerDiagnosticItem is not null)
                {
                    this.TreeService.DiagnosticContext.DiagnosticItems.Remove(this.missingAdornerLayerDiagnosticItem);
                }

                this.missingAdornerLayerDiagnosticItem = null;
                this.selectionHighlight?.Dispose();
                this.selectionHighlight = null;
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

            this.AddChild(this.TreeService.Construct(child, this));
        }

        if (this.Target is Grid grid
            // The logical tree already contains these elements
            && this.TreeService.TreeType != TreeType.Logical)
        {
            foreach (var row in grid.RowDefinitions)
            {
                this.AddChild(this.TreeService.Construct(row, this));
            }

            foreach (var column in grid.ColumnDefinitions)
            {
                this.AddChild(this.TreeService.Construct(column, this));
            }
        }
    }
}