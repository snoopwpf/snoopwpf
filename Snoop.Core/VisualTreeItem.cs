// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Text;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Media;
    using JetBrains.Annotations;

    public class VisualTreeItem : INotifyPropertyChanged
    {
        private bool isExpanded;
        private bool isSelected;

        private string name = string.Empty;
        private string nameLower = string.Empty;
        private string typeNameLower = string.Empty;
        private int visualChildrenCount;

        protected VisualTreeItem(object target, VisualTreeItem parent)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));

            this.Parent = parent;

            if (parent != null)
            {
                this.Depth = parent.Depth + 1;
            }
        }

        /// <summary>
        /// The WPF object that this VisualTreeItem is wrapping
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// The VisualTreeItem parent of this VisualTreeItem
        /// </summary>
        public VisualTreeItem Parent { get; }

        /// <summary>
        /// The depth (in the visual tree) of this VisualTreeItem
        /// </summary>
        public int Depth { get; }

        public string Name
        {
            get => this.name;

            private set
            {
                if (this.name == value)
                {
                    return;
                }

                // ensure that name never is null
                this.name = value ?? string.Empty;
                this.nameLower = this.name.ToLower();

                this.OnPropertyChanged(nameof(this.Name));
                this.OnPropertyChanged(nameof(this.DisplayName));
            }
        }

        public virtual string DisplayName => this.Name;

        public int SortOrder { get; protected set; }

        /// <summary>
        /// The VisualTreeItem children of this VisualTreeItem
        /// </summary>
        public ObservableCollection<VisualTreeItem> Children { get; } = new ObservableCollection<VisualTreeItem>();

        public bool IsSelected
        {
            get => this.isSelected;
            set
            {
                if (this.isSelected == value)
                {
                    return;
                }

                this.isSelected = value;

                // Need to expand all ancestors so this will be visible in the tree.
                if (this.isSelected)
                {
                    this.Parent?.ExpandTo();
                }

                this.OnPropertyChanged(nameof(this.IsSelected));
                this.OnSelectionChanged();
            }
        }

        /// <summary>
        /// Need this to databind to TreeView so we can expand our children.
        /// </summary>
        public bool IsExpanded
        {
            get => this.isExpanded;
            set
            {
                if (this.isExpanded == value)
                {
                    return;
                }

                this.isExpanded = value;
                this.OnPropertyChanged(nameof(this.IsExpanded));
            }
        }

        public virtual Visual MainVisual => null;

        public virtual Brush TreeBackgroundBrush => new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));

        public virtual Brush VisualBrush => null;

        /// <summary>
        /// Checks to see if any property on this element has a binding error.
        /// </summary>
        public virtual bool HasBindingError => false;

        public event PropertyChangedEventHandler PropertyChanged;

        public static VisualTreeItem Construct(object target, VisualTreeItem parent)
        {
            VisualTreeItem visualTreeItem;

            switch (target)
            {
                case Visual visual:
                    visualTreeItem = new VisualItem(visual, parent);
                    break;

                case ResourceDictionary resourceDictionary:
                    visualTreeItem = new ResourceDictionaryItem(resourceDictionary, parent);
                    break;

                case Application application:
                    visualTreeItem = new ApplicationTreeItem(application, parent);
                    break;

                default:
                    visualTreeItem = new VisualTreeItem(target, parent);
                    break;
            }

            visualTreeItem.Reload();

            return visualTreeItem;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(50);

            // [depth] name (type) numberOfChildren
            sb.AppendFormat("[{0:D3}] {1} ({2})", this.Depth, this.Name, this.Target.GetType().Name);

            if (this.visualChildrenCount != 0)
            {
                sb.Append(' ');
                sb.Append(this.visualChildrenCount.ToString());
            }

            return sb.ToString();
        }

        protected virtual void OnSelectionChanged()
        {
        }

        /// <summary>
        /// Expand this element and all elements leading to it.
        /// Used to show this element in the tree view.
        /// </summary>
        private void ExpandTo()
        {
            this.Parent?.ExpandTo();

            this.IsExpanded = true;
        }

        /// <summary>
        /// Update the view of this visual, rebuild children as necessary
        /// </summary>
        public void Reload()
        {
            this.GetName();

            this.typeNameLower = this.Target != null ? this.Target.GetType().Name.ToLower() : string.Empty;

            var toBeRemoved = new List<VisualTreeItem>(this.Children);

            this.Reload(toBeRemoved);

            foreach (var item in toBeRemoved)
            {
                this.RemoveChild(item);
            }

            // calculate the number of visual children
            foreach (var child in this.Children)
            {
                if (child is VisualItem)
                {
                    this.visualChildrenCount++;
                }

                this.visualChildrenCount += child.visualChildrenCount;
            }
        }

        protected virtual string GetName()
        {
            var name = string.Empty;

            if (this.Target is FrameworkElement targetFrameworkElement)
            {
                name = targetFrameworkElement.Name;

                if (string.IsNullOrEmpty(name))
                {
                    name = AutomationProperties.GetAutomationId(targetFrameworkElement);
                }
            }

            return name;
        }

        protected virtual void Reload(List<VisualTreeItem> toBeRemoved)
        {
        }

        public VisualTreeItem FindNode(object target)
        {
            // todo: it might be faster to have a map for the lookup check into this at some point

            if (this.Target == target)
            {
                return this;
            }

            foreach (var child in this.Children)
            {
                var node = child.FindNode(target);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Used for tree search.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Filter(string value)
        {
            if (this.typeNameLower.Contains(value))
            {
                return true;
            }

            if (this.nameLower.Contains(value))
            {
                return true;
            }

            if (int.TryParse(value, out var n)
                && n == this.Depth)
            {
                return true;
            }

            return false;
        }

        protected void RemoveChild(VisualTreeItem item)
        {
            item.IsSelected = false;
            this.Children.Remove(item);
        }

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}