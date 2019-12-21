# Changelog for Snoop

## 3.0.0 (preview)

- ### Bug fixes

  - [#45](../../issues/45) - Keystrokes go to Visual Studio main window when inspecting Visual Studio (thanks @KirillOsenkov)
  - [#101](../../issues/101) - My Style is being applied to the "Change Target" Window
  - [#150](../../issues/150) - Format and parse property values with the same format provider
  - [#151](../../issues/151) - Dependency properties are filtered wrong and less properties are shown than should be
  - [#156](../../issues/156) - Delve BindingExpression throws exception

- ### Improvements

  - You no longer have to have installed any Microsoft Visual C++ Redistributable(s)
  - Added a lot more tracing to the injection process. This tracing can be viewed with [DbgView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview).
  - Because of [#151](../../issues/151) there are now a lot more properties being shown.  
    As a way to reduce the noise a new option to filter uncommom properties was added. The default value for that is `true`.  
    If you want to show uncommon properties from types like `Typography` or `NumberSubstitution` etc. just disable the new switch right beside the default value switch.
  - [#98](../../issues/98) - .NETCore 3.0 support
  - [#108](../../issues/108) - SnoopWPF on "Disabled" control state?
  - [#139](../../issues/139) - Value Input did not support NewLine (\r\n)  
    This is achieved by a new detail value editor.
  - [#144](../../issues/144) - Add support for showing behaviors (added by @dezsiszabi in [#149](../../pull/149))

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