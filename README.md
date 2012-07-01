## Introduction

This is an enhanced version of the original Snoop which adds the scripting capabilities of PowerShell into the application.

## Notes

When cloning this repository, please ensure your core.autocrlf is set to true.

## Usage

By default, the PowerShell runspace will expose 2 variables:
 * $root
   * The root node of the TreeView.  This will typically be _App_ or _Popup_.
 * $current
   * This is the current selected item in the TreeView.  It is automatically updated when selection changes.

Next, a series of functions are provided out the box:
 * Reload-Scripts
   * This function will dot source reload all the scripts found in the *Scripts* folder that is found beside the Snoop.exe executable.  Some other functions like _Find-View_ and _Find-ViewModel_ will be found here, but if you want to write your own scripts, this is an easy way to reload all scripts (including yours) at runtime for testing.
 * Find-View -Name -Type
   * This function recursively finds every WPF control whose name matches the _name_ parameter, and optionally must also match the same _type_.
   * For example, "Find-View -Type ([System.Windows.Controls.Border])" will find every _Border_ in the current window.

## License

All original code and new code is released under the [Ms-PL](http://go.microsoft.com/fwlink/?LinkID=131993) license.
