// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Markup;

    public class ResourceDictionaryItem : VisualTreeItem
    {
        private readonly ResourceDictionary dictionary;

        public ResourceDictionaryItem(ResourceDictionary dictionary, VisualTreeItem parent) : base(dictionary, parent)
        {
            this.dictionary = dictionary;
        }

        public override string ToString()
        {
            return this.Children.Count + " Resources";
        }

        protected override void Reload(List<VisualTreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

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
    }

    public class ResourceItem : VisualTreeItem
    {
        private readonly object key;

        public ResourceItem(object target, object key, VisualTreeItem parent)
            : base(target, parent)
        {
            this.key = key;
        }

        public override string ToString()
        {
            return $"{this.key} ({this.Target.GetType().Name})";
        }
    }
}