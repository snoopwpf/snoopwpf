## Introduction

This is an enhanced version of the original Snoop which adds the scripting capabilities of PowerShell into the application.

## The Basics

By default, the PowerShell runspace will expose 4 variables:
 * $profile
   * This is the path to your SnoopProfile.ps1 profile which is automatically loaded on startup.
   * Search paths are %USERPROFILE%, followed by the _Scripts_ folder found alongside the binaries.
 * $root
   * The root node of the TreeView.  This will typically be _App_ or _Popup_.
 * $selected
   * This is the current selected item in the TreeView.  It is automatically updated when selection changes.
 * $ui
   * This is the instance of the Snoop WPF control.  This allows you to dynamically modify the UI such as adding menu items.

## Functions

 * Find-ByName($name,[switch]$select)
   * Performs a regex match on value of x:Name.
 * Find-ByType($type,[switch]$select)
   * Performs a regex match on the type name of the element.
 * Find-By([scriptblock]$predicate,[switch]$select)
   * Both the ByName and ByType variants are convenience functions which invokes this one.
   * The script block takes a single item, the VisualTreeItem, which can be filtered.

Note that in both cases, -Select will automatically select the first match in the tree view.

## Provider

There is preliminary support for a provider to navigate the tree (i.e., using cd, dir, etc.).  To enable this, enter the drive by issuing the command:  'cd snoop:'

## Hotkeys

 * F5: Invokes dot-sourcing the $profile variable, if it exists.
 * F12: Clears the output buffer.

## Notes

When cloning this repository, please ensure your core.autocrlf is set to true.

## License

All original code and new code is released under the [Ms-PL](http://go.microsoft.com/fwlink/?LinkID=131993) license.
