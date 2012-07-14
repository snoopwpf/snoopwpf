using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Snoop.Shell
{
    [CmdletProvider("VisualTreeProvider", ProviderCapabilities.None)]
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

        private static string GetPath(VisualTreeItem treeItem)
        {
            var path = "\\";
            var current = treeItem;
            while (current.Parent != null)
            {
                var name = current.Target.GetType().Name;
                path = "\\" + name + path;
                current = current.Parent;
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
                    var name = c.Target.GetType().Name;
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
                    var p = GetPath(c);
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
    }
}