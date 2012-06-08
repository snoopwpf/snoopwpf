// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Snoop
{
	public class ResourceDictionaryItem : VisualTreeItem
	{
		public ResourceDictionaryItem(ResourceDictionary dictionary, VisualTreeItem parent): base(dictionary, parent)
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

			foreach (object key in this.dictionary.Keys)
			{
				object target;
				try
				{
					target = this.dictionary[key];
				}
				catch (System.Windows.Markup.XamlParseException)
				{
					// sometimes you can get a XamlParseException ... because the xaml you are Snoop(ing) is bad.
					// e.g. I got this once when I was Snoop(ing) some xaml that was refering to an image resource that was no longer there.
					// in this case, just continue to the next resource in the dictionary.
					continue;
				}

				if (target == null)
				{
					// you only get a XamlParseException once. the next time through target just comes back null.
					// in this case, just continue to the next resource in the dictionary (as before).
					continue;
				}

				bool foundItem = false;
				foreach (VisualTreeItem item in toBeRemoved)
				{
					if (item.Target == target)
					{
						toBeRemoved.Remove(item);
						item.Reload();
						foundItem = true;
						break;
					}
				}

				if (!foundItem)
					this.Children.Add(new ResourceItem(target, key, this));
			}
		}

		private ResourceDictionary dictionary;
	}

	public class ResourceItem : VisualTreeItem
	{
		public ResourceItem(object target, object key, VisualTreeItem parent): base(target, parent)
		{
			this.key = key;
		}

		public override string ToString()
		{
			return this.key.ToString() + " (" + this.Target.GetType().Name + ")";
		}

		private object key;
	}
}
