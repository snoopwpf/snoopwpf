## Introduction

This is an enhanced version of the original Snoop which adds the scripting capabilities of PowerShell into the application.

## Requirements

Unlike the original version, this one is targeted against .NET 4 Client Profile.  It also depends on [PowerShell V3 RC](http://www.microsoft.com/en-us/download/details.aspx?id=29939) being installed.

## Usage

By default, the PowerShell runspace will expose 2 variables:
 * $root
   * The root node of the TreeView.  This will typically be _App_ or _Popup_.
 * $selected
   * This is the current selected item in the TreeView.  It is automatically updated when selection changes.
 * $modulePath
   * This is the full path to the Snoop.psm1 module.  This is useful if you are developing your own scripts and want to reload them on the fly.  Just issue a "Import-Module -Force $modulePath" and it will reload all scripts in the _Scripts_ folder alongside Snoop.exe.

Next, a series of functions are provided out the box:
 * Find-Item -Name -Type
   * This function recursively finds every WPF control whose name matches the _name_ parameter, and optionally must also match the same _type_.
   * For example, "Find-Item -Type ([System.Windows.Controls.Border])" will find every _Border_ in the current window.

## Notes

When cloning this repository, please ensure your core.autocrlf is set to true.

## License

All original code and new code is released under the [Ms-PL](http://go.microsoft.com/fwlink/?LinkID=131993) license.
