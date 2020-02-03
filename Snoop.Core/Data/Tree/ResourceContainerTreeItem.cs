// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree
{
    using System.Collections.Generic;
    using System.Windows;

    public abstract class ResourceContainerTreeItem : TreeItem
    {
        protected ResourceContainerTreeItem(object target, TreeItem parent)
            : base(target, parent)
        {
        }

        protected abstract ResourceDictionary ResourceDictionary { get; }

        protected override void Reload(List<TreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

            var resourceDictionary = this.ResourceDictionary;

            if (resourceDictionary != null
                && (resourceDictionary.Count != 0 || resourceDictionary.MergedDictionaries.Count > 0))
            {
                var foundItem = false;
                foreach (var item in toBeRemoved)
                {
                    if (item.Target == resourceDictionary)
                    {
                        toBeRemoved.Remove(item);
                        item.Reload();
                        foundItem = true;
                        break;
                    }
                }

                if (foundItem == false)
                {
                    this.Children.Add(Construct(resourceDictionary, this));
                }
            }
        }
    }
}