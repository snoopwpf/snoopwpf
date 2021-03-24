namespace Snoop.Data.Tree
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using JetBrains.Annotations;
    using Snoop.Infrastructure.Diagnostics;

    public enum TreeType
    {
        Visual,

        Logical,

        Automation
    }

    public abstract class TreeService : IDisposable, INotifyPropertyChanged
    {
        private TreeItem? rootTreeItem;

        public TreeService()
        {
            this.DiagnosticContext = new DiagnosticContext(this);
        }

        public abstract TreeType TreeType { get; }

        public TreeItem? RootTreeItem
        {
            get => this.rootTreeItem;
            set
            {
                if (Equals(value, this.rootTreeItem))
                {
                    return;
                }

                this.rootTreeItem = value;

                this.OnPropertyChanged();
            }
        }

        public DiagnosticContext DiagnosticContext { get; }

        public IEnumerable GetChildren(TreeItem treeItem)
        {
            if (treeItem.OmitChildren)
            {
                return Enumerable.Empty<object>();
            }

            return this.GetChildren(treeItem.Target);
        }

        public abstract IEnumerable GetChildren(object target);

        public virtual TreeItem Construct(object target, TreeItem? parent, bool omitChildren = false)
        {
            TreeItem treeItem;

            switch (target)
            {
                case AutomationPeer automationPeer:
                    treeItem = new AutomationPeerTreeItem(automationPeer, parent, this);
                    break;

                case ResourceDictionary resourceDictionary:
                    treeItem = new ResourceDictionaryTreeItem(resourceDictionary, parent, this);
                    break;

                case Application application:
                    treeItem = new ApplicationTreeItem(application, parent, this);
                    break;

                case Window window:
                    treeItem = new WindowTreeItem(window, parent, this);
                    break;

                case Popup popup:
                    treeItem = new PopupTreeItem(popup, parent, this);
                    break;

                case DependencyObject dependencyObject:
                    treeItem = new DependencyObjectTreeItem(dependencyObject, parent, this);
                    break;

                default:
                    treeItem = new TreeItem(target, parent, this);
                    break;
            }

            treeItem.OmitChildren = omitChildren;

            treeItem.Reload();

            if (parent is null)
            {
                // If the parent is null this should be our new root element
                this.RootTreeItem = treeItem;

                foreach (var child in treeItem.Children)
                {
                    if (child is ResourceDictionaryTreeItem)
                    {
                        continue;
                    }

                    child.ExpandTo();
                }
            }

            return treeItem;
        }

        public static TreeService From(TreeType treeType)
        {
            switch (treeType)
            {
                case TreeType.Visual:
                    return new VisualTreeService();

                case TreeType.Logical:
                    return new LogicalTreeService();

                case TreeType.Automation:
                    return new AutomationPeerTreeService();

                default:
                    throw new ArgumentOutOfRangeException(nameof(treeType), treeType, null);
            }
        }

        public void Dispose()
        {
            this.DiagnosticContext.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class VisualTreeService : TreeService
    {
        public override TreeType TreeType { get; } = TreeType.Visual;

        public override IEnumerable GetChildren(object target)
        {
            if (target is not DependencyObject dependencyObject
                || (target is Visual == false && target is Visual3D == false))
            {
                yield break;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(dependencyObject);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(dependencyObject, i);
                yield return child;
            }
        }
    }

    public sealed class LogicalTreeService : TreeService
    {
        public override TreeType TreeType { get; } = TreeType.Logical;

        public override IEnumerable GetChildren(object target)
        {
            if (target is not DependencyObject dependencyObject)
            {
                yield break;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(dependencyObject))
            {
                yield return child;
            }
        }
    }

    public sealed class AutomationPeerTreeService : TreeService
    {
        public override TreeType TreeType { get; } = TreeType.Automation;

        public override TreeItem Construct(object target, TreeItem? parent, bool omitChildren = false)
        {
            if (target is not AutomationPeer automationPeer
                && target is UIElement element)
            {
                target = UIElementAutomationPeer.CreatePeerForElement(element);
            }

            return base.Construct(target, parent, omitChildren: omitChildren);
        }

        public override IEnumerable GetChildren(object target)
        {
            if (target is not AutomationPeer automationPeer)
            {
                yield break;
            }

            var children = automationPeer.GetChildren();

            if (children is null)
            {
                yield break;
            }

            foreach (var child in children)
            {
                yield return child;
            }
        }
    }

    public sealed class AutomationElementTreeService : TreeService
    {
        private static readonly TreeWalker treeWalker = TreeWalker.ControlViewWalker;

        public override TreeType TreeType { get; } = TreeType.Automation;

        public override IEnumerable GetChildren(object target)
        {
            if (target is not AutomationElement automationElement)
            {
                yield break;
            }

            AutomationElement? child;
            try
            {
                child = treeWalker.GetFirstChild(automationElement);
            }
            catch (Exception)
            {
                yield break;
            }

            while (child is not null)
            {
                yield return child;

                try
                {
                    child = treeWalker.GetNextSibling(child);
                }
                catch (Exception)
                {
                    child = null;
                }
            }
        }
    }
}