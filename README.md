## Introduction

This is an enhanced version of the original Snoop which adds the scripting capabilities of PowerShell into the application.

## Usage

By default, the PowerShell runspace will expose 2 variables:
 * $root
   * The root node of the TreeView.  This will typically be _App_ or _Popup_.
 * $selected
   * This is the current selected item in the TreeView.  It is automatically updated when selection changes.

Next, a series of functions are provided out the box:
 * Find-Item -Name -Type
   * This function recursively finds every WPF control whose name matches the _name_ parameter, and optionally must also match the same _type_.
   * For example, "Find-Item -Type ([System.Windows.Controls.Border])" will find every _Border_ in the current window.

## Notes

When cloning this repository, please ensure your core.autocrlf is set to true.

## License

All original code and new code is released under the [Ms-PL](http://go.microsoft.com/fwlink/?LinkID=131993) license.
