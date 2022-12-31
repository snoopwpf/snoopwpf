// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree;

using System.Collections.Generic;
using System.Linq;

public abstract class ResourceContainerTreeItem : TreeItem
{
    protected ResourceContainerTreeItem(object target, TreeItem? parent, TreeService treeService)
        : base(target, parent, treeService)
    {
    }

    protected abstract IEnumerable<ResourceDictionaryWrapper?> ResourceDictionary { get; }

    protected override void ReloadCore()
    {
        var resourceDictionaries = this.ResourceDictionary.ToList();

        foreach (var resourceDictionary in resourceDictionaries)
        {
            if (resourceDictionary is null)
            {
                continue;
            }

            if (resourceDictionary.Keys.Count > 0
                || resourceDictionary.MergedDictionaries.Count > 0)
            {
                this.AddChild(this.TreeService.Construct(resourceDictionary, this));
            }
        }

        base.ReloadCore();
    }
}