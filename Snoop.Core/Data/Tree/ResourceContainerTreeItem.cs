// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree
{
    using System.Windows;

    public abstract class ResourceContainerTreeItem : TreeItem
    {
        protected ResourceContainerTreeItem(object target, TreeItem? parent, TreeService treeService)
            : base(target, parent, treeService)
        {
        }

        protected abstract ResourceDictionary? ResourceDictionary { get; }

        protected override void ReloadCore()
        {
            var resourceDictionary = this.ResourceDictionary;

            if (resourceDictionary is not null
                && (resourceDictionary.Count != 0 || resourceDictionary.MergedDictionaries.Count > 0))
            {
                this.Children.Add(this.TreeService.Construct(resourceDictionary, this));
            }

            base.ReloadCore();
        }
    }
}