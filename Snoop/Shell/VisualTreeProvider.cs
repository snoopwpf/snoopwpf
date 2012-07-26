using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Snoop.Shell
{
    [CmdletProvider("VisualTreeProvider", ProviderCapabilities.Filter)]
    public class VisualTreeProvider : NavigationCmdletProvider
    {
        private VisualTreeItem Root
        {
            get
            {
                var data = Host.PrivateData.BaseObject as Hashtable;
                if (data != null)
                {
                    return data["root"] as VisualTreeItem;
                }

                return null;
            }
        }

        private static string GetValidPath(string path)
        {
            path = path.Replace('/', '\\');
            if (!path.EndsWith("\\"))
            {
                path += '\\';
            }

            return path;
        }

        private VisualTreeItem GetTreeItem(string path)
        {
            path = GetValidPath(path);

            if (path.Equals("\\"))
            {
                return Root;
            }

            var parts = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var current = Root;
            var count = 0;
            foreach (var part in parts)
            {
                foreach (var c in current.Children)
                {
                    var name = c.NodeName();
                    if (name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    {
                        current = c;
                        count++;
                        break;
                    }
                }
            }

            if (count == parts.Length)
            {
                return current;
            }

            return null;
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            var item = GetTreeItem(path);
            if (item != null)
            {
                foreach (var c in item.Children)
                {
                    var p = c.NodePath();
                    GetItem(p);
                }
            }
            else
            {
                WriteWarning(path + " was not found.");
            }
        }

        protected override void GetItem(string path)
        {
            var item = GetTreeItem(path);
            WriteItemObject(item, path, true);
        }

        protected override bool HasChildItems(string path)
        {
            var item = GetTreeItem(path);
            return item.Children.Count > 0;
        }

        protected override bool IsItemContainer(string path)
        {
            return true;
        }

        protected override bool IsValidPath(string path)
        {
            path = GetValidPath(path);

            foreach (var c in path)
            {
                if (c == '/' || c == '\\')
                {
                    continue;
                }

                if (!char.IsLetter(c))
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool ItemExists(string path)
        {
            return GetTreeItem(path) != null;
        }

        protected override string GetChildName(string path)
        {
            return Path.GetFileName(path);
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var item = GetTreeItem(path);
            if (item != null)
            {
                foreach (var child in item.Children)
                {
                    var name = child.NodeName();
                    var nodePath = child.NodePath();
                    Debug.WriteLine(string.Format("{0}:: {1}", name, nodePath));
                    WriteItemObject(name, nodePath, true);
                }
            }
        }
    }

    internal static class VisualTreeProviderExtensions
    {
        public static string NodePath(this VisualTreeItem item)
        {
            var parts = new List<string>();
            
            var current = item;
            while (current.Parent != null)
            {
                var name = current.NodeName();
                parts.Insert(0, name);
                current = current.Parent;
            }

            return string.Join("\\", parts.ToArray());
        }

        public static string NodeName(this VisualTreeItem item)
        {
            var name = GetName(item);

            if (item.Parent != null)
            {
                var parent = item.Parent;
                var similarChildren = parent.Children.Where(c => GetName(c).Equals(name)).ToList();
                if (similarChildren.Count > 1)
                {
                    name += (similarChildren.IndexOf(item) + 1);
                }
            }

            return name;
        }

        private static string GetName(VisualTreeItem item)
        {
            return item.Target.GetType().Name;
        }
    }
}