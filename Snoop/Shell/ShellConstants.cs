// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using Microsoft.Win32;

namespace Snoop.Shell
{
    internal static class ShellConstants
    {
        /// <summary>
        /// The file name of the .psm1 module to load on startup.
        /// </summary>
        public const string SnoopModule = "Snoop.psm1";

        /// <summary>
        /// The file name of the .ps1 profile script to load on startup.
        /// </summary>
        public const string SnoopProfile = "SnoopProfile.ps1";

        /// <summary>
        /// Variable name for the root variable of the tree.
        /// </summary>
        public const string Root = "root";

        /// <summary>
        /// Variable name for the currently selected item in the tree.
        /// </summary>
        public const string Selected = "selected";

        /// <summary>
        /// Variable name for the path of a .ps1 script file which represents the profile of the current session.
        /// </summary>
        public const string Profile = "profile";

        /// <summary>
        /// The PowerShell provider drive name.
        /// </summary>
        public const string DriveName = "snoop";

        /// <summary>
        /// Gets the key for storing an <see cref="Action{T}"/> of type <see cref="VisualTreeItem"/>.
        /// </summary>
        public const string LocationChangedActionKey = "lca_key";

        /// <summary>
        /// Checks to see if PowerShell is installed.
        /// </summary>
        public static bool IsPowerShellInstalled
        {
            get
            {
                var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine");
                if (key != null)
                {
                    object keyValue = key.GetValue("PowerShellVersion");
                    if ("2.0".Equals(keyValue))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}