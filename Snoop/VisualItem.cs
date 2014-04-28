// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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
using DevExpress.Xpf.Core.Native;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core.Internal;
using System.Linq;

namespace Snoop
{
	/// <summary>
	/// Main class that represents a visual in the visual tree
	/// </summary>
	public class VisualItem : ResourceContainerItem
	{
		public VisualItem(object visual, VisualTreeItem parent): base(visual, parent)
		{
			this.visual = visual;
		}


		public object Visual
		{
			get { return this.visual; }
		}
		private object visual;


		public override bool HasBindingError
		{
			get
			{
				PropertyDescriptorCollection propertyDescriptors =
					TypeDescriptor.GetProperties(this.Visual, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });
				foreach (PropertyDescriptor property in propertyDescriptors)
				{
					DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property);
					if (dpd != null)
					{
						BindingExpressionBase expression = this.Visual is DependencyObject ?  BindingOperations.GetBindingExpressionBase((DependencyObject)this.Visual, dpd.DependencyProperty) : null;
						if (expression != null && (expression.HasError || expression.Status != BindingStatus.Active))
							return true;
					}
				}
				return false;
			}
		}
		public override object MainVisual
		{
			get { return this.Visual; }
		}
        public override Brush Foreground {
            get {
                if (Visual is Chrome)
                    return Brushes.Green;
                if (Visual is IFrameworkRenderElementContext)
                    return Brushes.Red;
                return base.Foreground;
            }
        }
		public override Brush TreeBackgroundBrush
		{
			get { return Brushes.Transparent; }
		}        
		public override Brush VisualBrush
		{
			get
			{
                VisualBrush brush = null;
                if (Visual is Visual)
                    brush = new VisualBrush((Visual)Visual);
                if (Visual is FrameworkRenderElementContext)
                    brush = new System.Windows.Media.VisualBrush(new FREDrawingVisual((FrameworkRenderElementContext)Visual));
                if (brush == null)
                    return null;

				brush.Stretch = Stretch.Uniform;
				return brush;
			}
		}


		protected override ResourceDictionary ResourceDictionary
		{
			get
			{
				FrameworkElement element = this.Visual as FrameworkElement;
				if (element != null)
					return element.Resources;
				return null;
			}
		}


		protected override void OnSelectionChanged()
		{
			// Add adorners for the visual this is representing.
            Visual visual_ = this.Visual as Visual;
            Thickness offset = new Thickness();
            if (visual_ == null) {
                var frec = this.Visual as FrameworkRenderElementContext;
                if (frec != null && frec.IsVisible()) {
                    visual_ = frec.ElementHost.Parent;
                    var transform = RenderTreeHelper.TransformToRoot(frec).Inverse;
                    var trrec = transform.TransformBounds(new Rect(frec.ElementHost.Parent.RenderSize));
                    offset = new Thickness(-trrec.Left, -trrec.Top, frec.ElementHost.Parent.RenderSize.Width - trrec.Right, frec.ElementHost.Parent.RenderSize.Height - trrec.Bottom);
                }

            }
            AdornerLayer adorners = visual_ == null ? null : AdornerLayer.GetAdornerLayer(visual_);
            UIElement visualElement = visual_ as UIElement;

			if (adorners != null && visualElement != null)
			{
				if (this.IsSelected && this.adorner == null)
				{
					Border border = new Border();
					border.BorderThickness = new Thickness(4);
                    border.Margin = offset;
					Color borderColor = new Color();
					borderColor.ScA = .3f;
					borderColor.ScR = 1;
					border.BorderBrush = new SolidColorBrush(borderColor);

					border.IsHitTestVisible = false;
					this.adorner = new AdornerContainer(visualElement);
					adorner.Child = border;
					adorners.Add(adorner);
				}
				else if (this.adorner != null)
				{
					adorners.Remove(this.adorner);
					this.adorner.Child = null;
					this.adorner = null;
				}
			}
		}
		protected override void Reload(List<VisualTreeItem> toBeRemoved)
		{
			// having the call to base.Reload here ... puts the application resources at the very top of the tree view.
			// this used to be at the bottom. putting it here makes it consistent (and easier to use) with ApplicationTreeItem
			base.Reload(toBeRemoved);

			// remove items that are no longer in tree, add new ones.
            for (int i = 0; i < CommonTreeHelper.GetChildrenCount(this.Visual); i++)
			{
                object child = CommonTreeHelper.GetChild(this.Visual, i);
				if (child != null)
				{
					bool foundItem = false;
					foreach (VisualTreeItem item in toBeRemoved)
					{
						if (item.Target == child)
						{
							toBeRemoved.Remove(item);
							item.Reload();
							foundItem = true;
							break;
						}
					}
					if (!foundItem)
						this.Children.Add(VisualTreeItem.Construct(child, this));
				}
			}

			Grid grid = this.Visual as Grid;
			if (grid != null)
			{
				foreach (RowDefinition row in grid.RowDefinitions)
					this.Children.Add(VisualTreeItem.Construct(row, this));
				foreach (ColumnDefinition column in grid.ColumnDefinitions)
					this.Children.Add(VisualTreeItem.Construct(column, this));
			}
		}


		private AdornerContainer adorner;
	}    
    public class FREDrawingVisual : DrawingVisual {
        static ReflectionHelper rHelper = new ReflectionHelper();
        IFrameworkRenderElementContext context = null;
        public FREDrawingVisual(FrameworkRenderElementContext context) {
            this.context = context;            
            using (var dc = this.RenderOpen()) {
                context.Factory.Render(dc, context);
                var controls = new FrameworkRenderElementContext[] { context }.Concat(RenderTreeHelper.RenderDescendants(context)).OfType<RenderControlBaseContext>();
                foreach(var ctrl in controls){
                    dc.PushTransform(ctrl.GeneralTransform);
                    dc.DrawRectangle(new VisualBrush(ctrl.Control), null, new Rect(new Point(0, 0), ctrl.RenderSize));
                    dc.Pop();
                }                
                dc.Close();
            }
        }        
    }
    public static class CommonTreeHelper {
        public static int GetChildrenCount(object source){
            if (source is IChrome) {
                var chrome = ((IChrome)source);
                if (chrome.Root != null)
                    return 1;
            }
            if (source is IFrameworkRenderElementContext) {
                int hasControl = (source as RenderControlBaseContext).With(x => x.Control).ReturnSuccess() ? 1 : 0;
                return ((IFrameworkRenderElementContext)source).RenderChildrenCount + hasControl;
            }
            if (source is Visual)
                return VisualTreeHelper.GetChildrenCount((Visual)source);
            return 0;
        }
        public static object GetChild(object source, int index) {
            if (source is IChrome) {
                var chrome = ((IChrome)source);
                if (index == 0 && chrome.Root != null)
                    return chrome.Root;
            }
            if (source is IFrameworkRenderElementContext) {
                var control = (source as RenderControlBaseContext).With(x => x.Control);
                var rcc = ((IFrameworkRenderElementContext)source).RenderChildrenCount;
                if (index >= rcc) {
                    if (index == rcc && control != null)
                        return control;
                    return null;
                }
                return ((IFrameworkRenderElementContext)source).GetRenderChild(index);
            }
            if (source is Visual)
                return VisualTreeHelper.GetChild((Visual)source, index);
            return null;
        }
        public static bool IsVisible(this FrameworkRenderElementContext context) {
            return isVisible(context) && RenderTreeHelper.RenderAncestors(context).All(x => isVisible(x));
        }
        static bool isVisible(FrameworkRenderElementContext context) {
            return context.Visibility.HasValue ? context.Visibility == Visibility.Visible : context.Factory.Visibility == Visibility.Visible;
        }
        public static bool IsDescendantOf(object visual, object rootVisual) {
            if (visual is Visual && rootVisual is Visual)
                return ((Visual)visual).IsDescendantOf((Visual)rootVisual);
            if (visual is FrameworkRenderElementContext && rootVisual is FrameworkRenderElementContext)
                return RenderTreeHelper.RenderAncestors(((FrameworkRenderElementContext)visual)).Any(x => x == rootVisual);
            if (visual is FrameworkRenderElementContext && rootVisual is Visual) {
                var frec = visual as FrameworkRenderElementContext;
                return frec.ElementHost.Parent.IsDescendantOf((Visual)rootVisual);
            }
            return false;
        }
    }
}
