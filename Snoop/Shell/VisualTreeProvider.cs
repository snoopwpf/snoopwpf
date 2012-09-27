// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Threading;

namespace Snoop.Shell
{
    [CmdletProvider("VisualTreeProvider", ProviderCapabilities.Filter)]
    public class VisualTreeProvider : NavigationCmdletProvider, IDisposable
    {
        internal const int LocationChangeNotifyDelay = 250;

        private VisualTreeItem Root
        {
            get
            {
                var data = (Hashtable)Host.PrivateData.BaseObject;
                return (VisualTreeItem)data[ShellConstants.Root];
            }
        }

        private readonly Timer selectedTimer;
        private string lastLocation;

        public VisualTreeProvider()
        {
            selectedTimer = new Timer(OnSyncSelectedItem, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void OnSyncSelectedItem(object _)
        {
            // the PSDrive.CurrentLocation gets set, but i couldn't find a way to have it notify
            // so unfortunately we have to poll :(
            if (this.PSDriveInfo.CurrentLocation != this.lastLocation)
            {
                var item = GetTreeItem(this.PSDriveInfo.CurrentLocation);

                if (item != null)
                {
                    var data = (Hashtable)Host.PrivateData.BaseObject;
                    var action = (Action<VisualTreeItem>)data[ShellConstants.LocationChangedActionKey];
                    action(item);
                }
                else
                {
                    // the visual tree changed drastically, we must reset the current location
                    this.PSDriveInfo.CurrentLocation = string.Empty;
                }

                this.lastLocation = this.PSDriveInfo.CurrentLocation;
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
                this.selectedTimer.Change(LocationChangeNotifyDelay, Timeout.Infinite);
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
                this.selectedTimer.Change(LocationChangeNotifyDelay, Timeout.Infinite);
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
            return item != null && item.Children.Count > 0;
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
                    WriteItemObject(name, nodePath, true);
                }
            }
        }

        public void Dispose()
        {
            this.selectedTimer.Dispose();
            GC.SuppressFinalize(this);
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