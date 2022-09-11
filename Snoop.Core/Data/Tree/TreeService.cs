namespace Snoop.Data.Tree;

using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
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
        TreeItem treeItem = target switch
        {
            AutomationPeer typedTarget => new AutomationPeerTreeItem(typedTarget, parent, this),
            ResourceDictionaryWrapper typedTarget => new ResourceDictionaryTreeItem(typedTarget, parent, this),
            ResourceDictionary typedTarget => new ResourceDictionaryTreeItem(typedTarget, parent, this),
            Application typedTarget => new ApplicationTreeItem(typedTarget, parent, this),
            Window typedTarget => new WindowTreeItem(typedTarget, parent, this),
            Popup typedTarget => new PopupTreeItem(typedTarget, parent, this),
            Image typedTarget => new ImageTreeItem(typedTarget, parent, this),
            DependencyObject typedTarget => this.ConstructFromDependencyObject(parent, typedTarget),
            _ => new TreeItem(target, parent, this)
        };

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

            return this.RootTreeItem;
        }

        return treeItem;
    }

    private DependencyObjectTreeItem ConstructFromDependencyObject(TreeItem? parent, DependencyObject dependencyObject)
    {
        if (WebBrowserTreeItem.IsWebBrowserWithDevToolsSupport(dependencyObject))
        {
            return new WebBrowserTreeItem(dependencyObject, parent, this);
        }

        return new DependencyObjectTreeItem(dependencyObject, parent, this);
    }

    public static TreeService From(TreeType treeType)
    {
        return treeType switch
        {
            TreeType.Visual => new VisualTreeService(),
            TreeType.Logical => new LogicalTreeService(),
            TreeType.Automation => new AutomationPeerTreeService(),
            _ => throw new ArgumentOutOfRangeException(nameof(treeType), treeType, null)
        };
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

        if (target is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                if (item is DependencyObject)
                {
                    continue;
                }

                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item);

                if (container is not null)
                {
                    yield return container;
                }
            }
        }
    }
}

public sealed class AutomationPeerTreeService : TreeService
{
    public override TreeType TreeType { get; } = TreeType.Automation;

    public override TreeItem Construct(object target, TreeItem? parent, bool omitChildren = false)
    {
        if (omitChildren == false
            && target is not AutomationPeer
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