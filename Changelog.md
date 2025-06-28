# Changelog for Snoop

## 6.1.0 (preview)

- ### Bug fixes

  - [#477](../../issues/477) - Binding Errors - Get error message

- ### Improvements

  - [#457](../../issues/457) - [Quality of Life] Synchronize Zoomer instance background slider value.

## 6.0.0

- ### Breaking changes

  - Dropped support for all .NET Framework versions prior to .NET 4.6.2
  - Dropped support for .NET 3.1 and 5
  - Dropped support for ARM (ARM64 is still supported)

- ### Bug fixes

  - [#397](../../issues/397) - Light mode broken
  - [#449](../../issues/449) - Cannot serialize a non-public type 'System.Windows.Controls.DataGridHeadersVisibilityToVisibilityConverter'.
  - [#450](../../issues/450) - Fix brush binding errors (thanks @Garzuuhl)
  - [#459](../../issues/459) - Issue with debugging applications that do not have an process path.
  - [#464](../../issues/464) - Fix Indent binding error on TreeViewItem (thanks @Garzuuhl)
  - Color values are now displayed with the same width as brushes
  - Instances of classes are no longer created during property discovery
  - Fixed detection of read only properties
  - Fixed loaded of settings

- ### Improvements

  - [#435](../../issues/435) - Add "StartMinimized" Setting + Functionality (thanks @BButner)
  - [#436](../../issues/436) - [Feature request] show current version
  - [#453](../../issues/453) - Better target windows titles (thanks @miloush)
  - [#462](../../issues/462) - Optimize CurrentProcessPath for .NET (thanks @kasperk81)
  - [#467](../../issues/467) - Allow to ignore elements with IsHitTestVisible="false" (thanks @Laniusexcubitor)
  - Improved property filter
    - Property values are now included when filtering
    - Regex is now explicit instead of implicit
  - Improved performance for attached properties
  - Improved performance of style retrieval
  - Improved performance of resource key retrieval

## 5.1.0

- ### Bug fixes

  - [#417](../../issues/417) - StackOverflowException on Ctrl+Shift (thanks @miloush)

- ### Improvements

  - Improved colors in dark theme
  - [#414](../../issues/414) - Feature Request: Option to snoop without activating snoop on global shortcut
  - [#416](../../issues/416) - Copy delve type name into clipboard (thanks @miloush)

## 5.0.2

- ### Bug fixes

  - [#405](../../issues/405) - After upgrading my app to .net 8 snoop no longer works with it

## 5.0.1

- ### Bug fixes

  - [#399](../../issues/399) - Save the Current Preview to file does not work all the time
  - [#405](../../issues/405) - After upgrading my app to .net 8 snoop no longer works with it

- ### Improvements

  - [#404](../../issues/404) - Snoop does not show version

## 5.0.0

- ### Breaking changes

  - Dropped support for all .NET Framework versions prior to .NET 4.5.2
  - Dropped support for .NET 3.0
  - Added support for .NET versions >= 6 (by not explicitly blocking versions greater than 6)
  - [#316](../../issues/316) - Improved settings management and storage  
    Settings do not rely on `System.Configuration` anymore.  
    The new system allows sharing of settings between different snooped applications.  
    It also allows to define settings for whole directory trees.

- ### Bug fixes

  - [#300](../../issues/300) - An error has occured in developing .NET 6 Desktop App
  - [#313](../../issues/313) - Error: Collection was modified; enumeration operation may not execute.
  - [#318](../../issues/318) - Styles from application apply to Snoop UI if DefaultStyleKey is overwritten by application
  - [#319](../../issues/319) - Wrong style being displayed in property inspector if DefaultStyleKey is overwritten.
  - [#333](../../issues/333) - Dual Monitor high dpi window sizes and positions broken (thanks @Algorithman)
  - [#374](../../issues/374) - Unhandled InvalidCastException when running on .NET 7

- ### Improvements

  - Editing `Color?` and `Enum?` values works now
  - Improved resource lookup (used to get resource keys for resources)
  - Added dark theme
  - [#278](../../issues/278) - Adorner Layer not visible on certain controls (Snoop now reports a diagnostic error when there is no adorner layer for the selected element)
  - [#283](../../issues/283) - [Feature Request] Be able to import filters or make them available across applications. (solved by [#316](../../issues/316))
  - [#314](../../issues/314) - Hide properties from Snoop?  
    You can now hide properties from Snoop by adding `[System.ComponentModel.BrowsableAttribute(false)]` to your property.  
    It's only shown then if the "Show uncommon properties" is enabled.
  - [#320](../../issues/320) - System resources are not shown in the tree
  - [#326](../../issues/326) - Enable Snoop to show the dev tools of browser controls
  - [#339](../../issues/339) - Value selector when dependency property type is a nullable enum.

## 4.0.1

- ### Bug fixes

  - Editing values works again

## 4.0.0

- ### Breaking changes

  - Dropped support for all .NET versions prior to .NET 4.5.1

- ### Bug fixes

  - Path for entries from `ResourceDictionary` is now displayed correctly when delving 
  - Detaching Snoop now properly pops the menu mode. Prior to this certain keyboard keys, like DEL or LEFT or RIGHT etc., stopped working.
  - Detaching Snoop now properly detaches it's exception handler
  - Fixed a performance regression in the window finder when using mouse cursor drop
  - Fixed an exception when application contains invalid resource definitions
  - Suppressed exceptions while trying to get property information
  - [#220](../../issues/220) - StackOverflowException in ProperTreeViewItem.ArrangeOverride
  - [#221](../../issues/221) - DPI aware Issue?
  - [#232](../../issues/232) - System.NotSupportedException
  - [#252](../../issues/252) - Display Scaling
  - [#254](../../issues/254) - Exception at SnoopMainBaseWindow.FindRoot() with background dispatcher hosted visual
  - [#266](../../issues/266) - Out of memory exception in snoop after target app was converted from .NET 4.8 to .NET 5

- ### Improvements

  - Maximum displayed events in events viewer are now persisted in settings
  - Added menu items to close the current snoop window, open the folder containing the settings for the currently running application and reset the current settings
  - Added support for `ThreeState` bool values in the properties grid
  - Tracking mode change: Holding CTRL is replaced by a mode setting as it was triggered all the time when copying text, switching tabs etc..
  - Support for ARM/ARM64
  - Support for .NET 5 and .NET 6
  - Binding errors are now resolved in an explicit lazy way to prevent it from fixing the error silently.  
    Starting with .NET 5 binding errors are resolved automatically in most cases by using the new `BindingDiagnostics` class from WPF.
  - Made `Color` properties editable
  - [#38](../../issues/38) - Export tree (thanks @amake for the basic idea and starting point)
  - [#103](../../issues/103) - Feature Request: Persist Tracked Events Settings
  - [#210](../../issues/210) - Add dedicated "Diagnostics" view
  - [#212](../../issues/212) - Add binding diagnostics
  - [#213](../../issues/213) - Add DynamicResource diagnostics
  - [#219](../../issues/219) - Add a warning to zoomer if target has TextOptions.TextFormattingMode=Display
  - [#226](../../issues/226) - Add support for ARM/ARM64
  - [#227](../../issues/227) - Add support for .NET 6
  - [#279](../../issues/279) - Improve filter box tooltip
  - [#285](../../issues/285) - Improvements to the highlight adorner

## 3.0.1

- ### Bug fixes

  - Fixing window finder cursor display when DPI != 100%
  - [#203](../../issues/203) - The calling thread cannot access this object because a different thread owns it.
  - [#207](../../issues/207) - Exception when trying to snoop application with invalid resource definitions inside ResourceDictionary
  
## 3.0.0

- ### Bug fixes

  - [#40](../../issues/40) - Message: Cannot set Expression. It is marked as 'NonShareable' and has already been used.
  - [#45](../../issues/45) - Keystrokes go to Visual Studio main window when inspecting Visual Studio (thanks @KirillOsenkov)
  - [#66](../../issues/66) - System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
  - [#101](../../issues/101) - My Style is being applied to the "Change Target" Window
  - [#120](../../issues/120) - Screenshot feature produces pixelated low-res image for larger windows
  - [#150](../../issues/150) - Format and parse property values with the same format provider
  - [#151](../../issues/151) - Dependency properties are filtered wrong and less properties are shown than should be
  - [#152](../../issues/152) - Magnified view only works for main window
  - [#156](../../issues/156) - Delve BindingExpression throws exception
  - [#159](../../issues/159) - Errors require STA
  - [#177](../../issues/177) - Could not query process information.
  - [#188](../../issues/188) - Crash when inspecting app with VisualTargetPresentationSource from ModernWpf

  - Snoop now properly selects the targeted window on startup
  - Snooping multiple app domains now also works for app domains that use shadow copies of assemblies
  - Snooping multiple app domains now also checks for multiple dispatchers in each app domain

- ### Improvements

  - You no longer have to have installed any Microsoft Visual C++ Redistributable(s)
  - Added a lot more tracing to the injection process. This tracing can be viewed with [DbgView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview).
  - Because of [#151](../../issues/151) there are now a lot more properties being shown.  
    As a way to reduce the noise a new option to filter uncommom properties was added. The default value for that is `true`, so uncommon properties are hidden by default.  
    If you want to show uncommon properties from types like `Typography` or `NumberSubstitution` etc. just disable the new switch right beside the default value switch.
  - Added "Copy XAML" to the context menu of the property grid. Please note that this feature is not finished and the generated XAML is not very good. I hope to improve this in the future.
  - [#82](../../issues/82) - Missing possibility of copying value of the specific node
  - [#98](../../issues/98) - .NETCore 3.0 support
  - [#108](../../issues/108) - SnoopWPF on "Disabled" control state?
  - [#129](../../issues/129) - Command line args
  - [#139](../../issues/139) - Value Input did not support NewLine (\r\n)  
    This is achieved by a new detail value editor.
  - [#140](../../issues/140) - CTRL_SHIFT stops working
  - [#141](../../issues/141) - Add support to view logical tree
  - [#142](../../issues/142) - Add support to view ui automation tree (wpf automation peers)
  - [#144](../../issues/144) - Add support for showing behaviors (added by @dezsiszabi in [#149](../../pull/149))
  - Snoop now filters uncommon properties by default
  - Snoop is now able to show `MergedDictionaries` from `ResourceDictionary`
  - Snoop now has two tracking modes.
    - Holding CTRL tries to skip template parts
    - Holding CTRL + SHIFT does not skip template parts
  - [#161](../../issues/161) - Drastically improved performance of AppChooser.Refresh() (thanks @mikel785)
  - [#162](../../issues/162) - Usability improvements for process dropdown (thanks @mikel785)
  - [#181](../../issues/181) - Add inspection of Popup without opening it
  - [#190](../../issues/190) - Events view - editible events history max count (thanks @X39)

## 2.11.0

- ### Bug fixes
  
  - [#53](../../issues/53) - Path Data values have wrong format (should use invariant culture) (thanks @jongleur1983)
  - [#55](../../issues/55) - Keyboard events not passed to snoop UI window (thanks @stutton)
  - [#56](../../issues/56) - Snoop crash when application shutdown (solved by using System.Windows.Forms.Clipboard)
  - [#83](../../issues/83) - Unhandled Exception when changing WPF Trace Level to Activity Tracing (thanks @miloush)
  - [#86](../../issues/86) - Fatal ExecutionEngineException when process has hidden windows without composition target (thanks @gix)
  - [#99](../../issues/99) - Prevent window from being restored on screen that's disconnected/off
  - [#100](../../issues/100) - Snoop 2.10 crashes when snooping a WPF App that uses AvalonDock
  - [#106](../../issues/106) - Refresh fails because "process has exited" (thanks @jmbeach)

- ### Improvements

  - [#32](../../issues/32) - Try to use `AutomationProperties.AutomationId` for `VisualTreeItem` name if element name is not specified. (thanks @paulspiteri)
  - [#73](../../issues/73) - Add options to prevent multiple dispatcher question and setting of owner on snoop windows
  - [#89](../../issues/89) - Improved exception handling and error dialog
  - [#92](../../issues/92) - Adding support for snooping elevated processes from a non elevated snoop instance
  - [#116](../../issues/116) - Doesn't find PresentationSource hosted in CustomTaskPane (ElementHost) in Office VSTO Add-in  
  This means snoop is now able to spy on multiple app domains.
  - [#119](../../issues/119) - Adding hyperlink for current delve object to enable explorer navigation
  - The window finder was rewritten to not use a separate window but a dynamically generated mouse cursor instead

## 2.10.0

- ### Breaking changes
  
  - Dropped support for .NET 3.5
  - You now need Visual C++ 2015 Runtime to run snoop

## 2.9.0

- ### Improvements
  
  - Added a new triggers tab to view triggers from ControlTemplates and Styles
  