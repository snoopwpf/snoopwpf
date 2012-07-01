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

Next, a series of functions are provided out the box:
 * Reload-Scripts
   * This function will dot source reload all the scripts found in the *Scripts* folder that is found beside the Snoop.exe executable.  Some other functions like _Find-View_ and _Find-ViewModel_ will be found here, but if you want to write your own scripts, this is an easy way to reload all scripts (including yours) at runtime for testing.
 * Find-Item -Name -Type
   * This function recursively finds every WPF control whose name matches the _name_ parameter, and optionally must also match the same _type_.
   * For example, "Find-Item -Type ([System.Windows.Controls.Border])" will find every _Border_ in the current window.

## Notes

When cloning this repository, please ensure your core.autocrlf is set to true.

## License

All original code and new code is released under the [Ms-PL](http://go.microsoft.com/fwlink/?LinkID=131993) license.
