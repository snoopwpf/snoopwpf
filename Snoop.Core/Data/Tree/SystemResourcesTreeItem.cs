namespace Snoop.Data.Tree;

using System.Collections;
using System.Reflection;
using System.Windows;

public sealed class SystemResourcesTreeItem : TreeItem
{
    private TreeItem? placeholderChild;

    public SystemResourcesTreeItem(TreeItem? parent, TreeService treeService)
        : base("System resources", parent, treeService)
    {
        this.ShouldBeAnalyzed = false;
    }

    public override string DisplayName => "System resources";

    public override string ToString()
    {
        return this.DisplayName;
    }

    protected override void ReloadCore()
    {
        if (this.IsExpanded == false)
        {
            this.placeholderChild = new TreeItem("Placeholder", this, this.TreeService);
            this.AddChild(this.placeholderChild);

            return;
        }

        this.ReallyLoadChildren();
    }

    private void ReallyLoadChildren()
    {
        if (this.placeholderChild is not null)
        {
            if (this.RemoveChild(this.placeholderChild) == false)
            {
                return;
            }
        }

        var type = typeof(ResourceDictionary).Assembly.GetType("System.Windows.SystemResources");

        if (type is null)
        {
            return;
        }

        var dictionariesField = type.GetField("_dictionaries", BindingFlags.Static | BindingFlags.NonPublic)
                                ?? type.GetField("dictionaries", BindingFlags.Static | BindingFlags.NonPublic);

        var dictionaries = (IDictionary?)dictionariesField?.GetValue(null);

        if (dictionaries is null)
        {
            return;
        }

#pragma warning disable CS8605
        foreach (DictionaryEntry dictionariesEntry in dictionaries)
#pragma warning restore CS8605
        {
            var assembly = (Assembly)dictionariesEntry.Key;
            var resourceDictionaries = dictionariesEntry.Value;

            if (resourceDictionaries is null)
            {
                continue;
            }

            var genericDictionaryField = resourceDictionaries.GetType().GetField("_genericDictionary", BindingFlags.Instance | BindingFlags.NonPublic)
                                         ?? resourceDictionaries.GetType().GetField("genericDictionary", BindingFlags.Instance | BindingFlags.NonPublic);

            var genericDictionary = (ResourceDictionary?)genericDictionaryField?.GetValue(resourceDictionaries);

            if (genericDictionary is not null)
            {
                this.AddChild(new SystemResourceTreeItem(assembly, genericDictionary, this, this.TreeService).Reload());
            }

            var themedDictionaryField = resourceDictionaries.GetType().GetField("_themedDictionary", BindingFlags.Instance | BindingFlags.NonPublic)
                                        ?? resourceDictionaries.GetType().GetField("themedDictionary", BindingFlags.Instance | BindingFlags.NonPublic);

            var themedDictionary = (ResourceDictionary?)themedDictionaryField?.GetValue(resourceDictionaries);

            if (themedDictionary is not null)
            {
                this.AddChild(new SystemResourceTreeItem(assembly, themedDictionary, this, this.TreeService).Reload());
            }
        }
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(this.IsExpanded)
            && this.IsExpanded)
        {
            this.ReallyLoadChildren();
        }
    }
}