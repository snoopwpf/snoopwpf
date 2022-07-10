// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma warning disable CA1001

namespace Snoop.PowerShell;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Threading;
using Snoop.Data.Tree;

[CmdletProvider(nameof(VisualTreeProvider), ProviderCapabilities.Filter)]
public class VisualTreeProvider : NavigationCmdletProvider
{
    private Timer? oneTimeSyncTimer;
    private const int LocationChangeNotifyDelay = 250;

    private TreeItem? Root
    {
        get
        {
            var data = (Hashtable)this.Host.PrivateData.BaseObject;
            return (TreeItem?)data[ShellConstants.Root];
        }
    }

    private string? LastKnownSnoopLocation
    {
        get
        {
            var data = (Hashtable)this.Host.PrivateData.BaseObject;
            return (string?)data[ShellConstants.LastKnownSnoopLocation];
        }

        set
        {
            var data = (Hashtable)this.Host.PrivateData.BaseObject;
            data[ShellConstants.LastKnownSnoopLocation] = value;
        }
    }

    public bool IsCurrentlySynchingLastKnownSnoopLocation
    {
        get
        {
            var data = (Hashtable)this.Host.PrivateData.BaseObject;
            return ((bool?)data[ShellConstants.IsCurrentlySynchingLastKnownSnoopLocation]).GetValueOrDefault(false);
        }

        set
        {
            var data = (Hashtable)this.Host.PrivateData.BaseObject;
            data[ShellConstants.IsCurrentlySynchingLastKnownSnoopLocation] = value;
        }
    }

    private void OnSyncSelectedItem(object? currentTryObj)
    {
        var currentTry = (int)currentTryObj!;

        if (currentTry >= 5)
        {
            return;
        }

        if (currentTry == 0
            && this.IsCurrentlySynchingLastKnownSnoopLocation)
        {
            return;
        }

        this.IsCurrentlySynchingLastKnownSnoopLocation = true;

        try
        {
            // the PSDrive.CurrentLocation gets set, but i couldn't find a way to have it notify
            // so unfortunately we have to poll :(
            if (this.PSDriveInfo.CurrentLocation != this.LastKnownSnoopLocation)
            {
                var item = this.GetTreeItem(this.PSDriveInfo.CurrentLocation);

                if (item is not null)
                {
                    var data = (Hashtable)this.Host.PrivateData.BaseObject;
                    var action = (Action<TreeItem>?)data[ShellConstants.LocationChangedActionKey];
                    action?.Invoke(item);
                }
                else
                {
                    // the visual tree changed drastically, we must reset the current location
                    this.PSDriveInfo.CurrentLocation = string.Empty;
                }

                this.LastKnownSnoopLocation = this.PSDriveInfo.CurrentLocation;
            }
        }
        catch
        {
            this.oneTimeSyncTimer?.Dispose();
            this.oneTimeSyncTimer = null;

            this.StartNewOneTimeSyncTimer(++currentTry);
        }
        finally
        {
            this.oneTimeSyncTimer?.Dispose();
            this.oneTimeSyncTimer = null;

            this.IsCurrentlySynchingLastKnownSnoopLocation = false;
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

    private TreeItem? GetTreeItem(string path)
    {
        path = GetValidPath(path);

        if (path.Equals("\\", StringComparison.Ordinal))
        {
            this.StartNewOneTimeSyncTimer();
            return this.Root;
        }

        var parts = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var current = this.Root;

        if (current is null)
        {
            return null;
        }

        var count = 0;
        foreach (var part in parts)
        {
            foreach (var c in current.Children.ToList())
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
            this.StartNewOneTimeSyncTimer();
            return current;
        }

        return null;
    }

    private void StartNewOneTimeSyncTimer(int currentTry = 0)
    {
        if (this.oneTimeSyncTimer is not null)
        {
            return;
        }

        this.oneTimeSyncTimer = new Timer(this.OnSyncSelectedItem, currentTry, LocationChangeNotifyDelay, Timeout.Infinite);
    }

    protected override ProviderInfo Start(ProviderInfo providerInfo)
    {
        providerInfo.Home = $"{ShellConstants.DriveName}:\\";

        return base.Start(providerInfo);
    }

    protected override Collection<PSDriveInfo> InitializeDefaultDrives()
    {
        var drives = base.InitializeDefaultDrives();
        var snoopDriveInfo = new PSDriveInfo(ShellConstants.DriveName, this.ProviderInfo, "/", string.Empty, null);
        drives.Add(snoopDriveInfo);
        return drives;
    }

    protected override void GetChildItems(string path, bool recurse)
    {
        var item = this.GetTreeItem(path);
        if (item is not null)
        {
            foreach (var c in item.Children.ToList())
            {
                var p = c.NodePath();
                this.GetItem(p);
            }
        }
        else
        {
            this.WriteWarning(path + " was not found.");
        }
    }

    protected override void GetItem(string path)
    {
        var item = this.GetTreeItem(path);
        this.WriteItemObject(item, path, true);
    }

    protected override bool HasChildItems(string path)
    {
        var item = this.GetTreeItem(path);
        return item is not null
               && item.Children.Any();
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
        return this.GetTreeItem(path) is not null;
    }

    protected override string GetChildName(string path)
    {
        return Path.GetFileName(path);
    }

    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        var item = this.GetTreeItem(path);
        if (item is not null)
        {
            foreach (var child in item.Children.ToList())
            {
                var name = child.NodeName();
                var nodePath = child.NodePath();
                this.WriteItemObject(name, nodePath, true);
            }
        }
    }
}

internal static class TreeItemExtensions
{
    public static string NodePath(this TreeItem item)
    {
        var parts = new List<string>();

        var current = item;
        while (current.Parent is not null)
        {
            var name = current.NodeName();
            parts.Insert(0, name);
            current = current.Parent;
        }

        return string.Join("\\", parts.ToArray());
    }

    public static string NodeName(this TreeItem item)
    {
        var name = GetName(item);

        if (item.Parent is not null)
        {
            var parent = item.Parent;
            var similarChildren = parent.Children.ToList()
                .Where(c => GetName(c).Equals(name, StringComparison.Ordinal))
                .ToList();
            if (similarChildren.Count > 1)
            {
                name += similarChildren.IndexOf(item) + 1;
            }
        }

        return name;
    }

    private static string GetName(TreeItem item)
    {
        return item.Target.GetType().Name;
    }
}