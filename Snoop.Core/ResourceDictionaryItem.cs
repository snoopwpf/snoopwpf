// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Markup;

    public class ResourceDictionaryItem : TreeItem
    {
        private static readonly SortDescription dictionarySortDescription = new SortDescription(nameof(SortOrder), ListSortDirection.Ascending);
        private static readonly SortDescription displayNameSortDescription = new SortDescription(nameof(DisplayName), ListSortDirection.Ascending);

        private readonly ResourceDictionary dictionary;

        public ResourceDictionaryItem(ResourceDictionary dictionary, TreeItem parent)
            : base(dictionary, parent)
        {
            this.dictionary = dictionary;

            var childrenView = CollectionViewSource.GetDefaultView(this.Children);
            childrenView.SortDescriptions.Add(dictionarySortDescription);
            childrenView.SortDescriptions.Add(displayNameSortDescription);
        }

        public override TreeItem FindNode(object target)
        {
            return null;
        }

        protected override string GetName()
        {
            var source = this.dictionary.Source?.ToString();

            if (string.IsNullOrEmpty(source))
            {
                return base.GetName();
            }

            return source;
        }

        protected override void Reload(List<TreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

            var order = 0;
            foreach (var mergedDictionary in this.dictionary.MergedDictionaries)
            {
                var resourceDictionaryItem = new ResourceDictionaryItem(mergedDictionary, this)
                {
                    SortOrder = order
                };
                resourceDictionaryItem.Reload();

                this.Children.Add(resourceDictionaryItem);

                ++order;
            }

            foreach (var key in this.dictionary.Keys)
            {
                object target;
                try
                {
                    target = this.dictionary[key];
                }
                catch (XamlParseException)
                {
                    // sometimes you can get a XamlParseException ... because the xaml you are Snoop(ing) is bad.
                    // e.g. I got this once when I was Snoop(ing) some xaml that was referring to an image resource that was no longer there.
                    // in this case, just continue to the next resource in the dictionary.
                    continue;
                }

                if (target == null)
                {
                    // you only get a XamlParseException once. the next time through target just comes back null.
                    // in this case, just continue to the next resource in the dictionary (as before).
                    continue;
                }

                var foundItem = false;
                foreach (var item in toBeRemoved)
                {
                    if (item.Target == target)
                    {
                        toBeRemoved.Remove(item);
                        item.Reload();
                        foundItem = true;
                        break;
                    }
                }

                if (foundItem == false)
                {
                    this.Children.Add(new ResourceItem(target, key, this));
                }
            }
        }

        public override string ToString()
        {
            var source = this.dictionary.Source?.ToString();

            if (string.IsNullOrEmpty(source))
            {
                return $"{this.Children.Count} resources";
            }

            return $"{this.Children.Count} resources ({source})";
        }
    }

    public class ResourceItem : TreeItem
    {
        private readonly object key;

        public ResourceItem(object target, object key, TreeItem parent)
            : base(target, parent)
        {
            this.key = key;
            this.SortOrder = int.MaxValue;
        }

        public override string DisplayName => this.key.ToString();

        public override string ToString()
        {
            return $"{this.key} ({this.Target.GetType().Name})";
        }
    }
}