# Snoop

Snoop is an open source WPF spying utility originally created by [Pete Blois](https://github.com/peteblois) and is currently maintained by [Bastian Schmidt](https://github.com/batzen).

It allows you to spy/browse the visual, logical and automation tree of any running WPF application (without the need for a debugger).  
You can change property values, view triggers, set breakpoints on property changes and many more things.

[![Build status for master branch](https://img.shields.io/appveyor/ci/batzen/snoopwpf/master?style=flat-square&&label=master)](https://ci.appveyor.com/project/batzen/snoopwpf/branch/master)
[![Build status for develop branch](https://img.shields.io/appveyor/ci/batzen/snoopwpf/develop?style=flat-square&&label=develop)](https://ci.appveyor.com/project/batzen/snoopwpf/branch/develop)
[![chocolatey version](http://img.shields.io/chocolatey/v/snoop.svg?style=flat-square)](https://chocolatey.org/packages/snoop)
[![chocolatey download count](http://img.shields.io/chocolatey/dt/snoop.svg?style=flat-square)](https://chocolatey.org/packages/snoop)

## Contact

- [![Join the chat at https://gitter.im/snoopwpf/Lobby](https://img.shields.io/badge/GITTER-join%20chat-green.svg?style=flat-square)](https://gitter.im/snoopwpf/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
- [![Twitter](https://img.shields.io/badge/twitter-%40batzendev-blue.svg?style=flat-square)](https://twitter.com/batzendev)

## Where can i download Snoop?/How can i install Snoop?

- [chocolatey](https://chocolatey.org/packages/snoop) for stable and some preview versions
- [github releases](https://github.com/snoopwpf/snoopwpf/releases) for stable versions
- [appveyor](https://ci.appveyor.com/project/batzen/snoopwpf/build/artifacts) for the latest preview versions (built on every code change)
- You need at least .NET Framework 4.0 to run Snoop

## Versions

You can read the [changelog](Changelog.md) for details on what changed in which version.

### [3.0.0](../../releases/tag/v3.0.0)

Highlights:

- Support for .NET Core (3.0, 3.1 and 5.0) (including self contained and single file applications)
- Rewritten injector code
- You no longer have to have installed any Microsoft Visual C++ Redistributable(s)
- Snooping disabled controls when holding `CTRL + SHIFT` works now
- Snoop now filters uncommon properties by default
- Snoop is now able to show `MergedDictionaries` from `ResourceDictionary`
- Snoop now has two tracking modes.
  - Holding `CTRL` tries to skip template parts => this is changed to `CTRL + ALT` in newer versions
  - Holding `CTRL + SHIFT` does not skip template parts
- Drastically improved performance of `AppChooser.Refresh()` (thanks @mikel785)
- Usability improvements for process dropdown (thanks @mikel785)
- Support for displaying the logical tree and the tree of WPF automation peers
- Ability to inspect `Popup` without opening it
- `Snoop.exe` and the injector launcher now support commandline args
- Global hotkey support (just start snoop, focus a WPF application and hit `CTRL + WIN + ALT + F12`)

Known issues:

- Trying to snoop a trimmed single file application might not work as trimming might have removed things Snoop relies on

### [2.11.0](../../releases/tag/2.11.0)

Highlights:

- Support for multiple app domains
- Auto elevation to enable spying of elevated processes without running Snoop as administrator
- Persistent settings for various settings
- Improved error dialog and issue reporting
- Rewritten window finder

### [2.10.0](../../releases/tag/2.10.0)

Was released on September 19th, 2018.
In this version we finally got rid of support for snooping WPF 3.5 applications.
This allowed us to move the Snoop projects forward to Visual Studio 2017 which should make it much easier to work with Snoop's source code.

### [2.9.0](../../releases/tag/2.9.0)

Was released on July 27th, 2018.
The big addition in this version was the inclusion of the triggers tab which was a useful feature of another WPF spying utility called WPF Inspector (written by [Christan Moser](https://github.com/ChristianMoser)).
It was ported to Snoop by Bastian Schmidt.

## Documentation on how to use Snoop

Unfortunately there isn't any exhaustive documentation on how to use Snoop and there are plenty of hidden features. If someone is willing to work on this, please let me know. On the bright side, it is a pretty easy utility to use and learn. I have made three videos which should get most people quick started.

Here are the links to the current Snoop Tips & Tricks:

- http://www.cplotts.com/2011/02/10/snoop-tips-tricks-1-ctrl-shift-mouse-over
- http://www.cplotts.com/2011/02/14/snoop-tips-tricks-2-snooping-transient-visuals
- http://www.cplotts.com/2012/05/31/snoop-tips-tricks-3-the-crosshairs

## Why can't I snoop .NET 3.5 applications?

Well, you can! You will just need to use Snoop 2.9.0 and earlier, in order to do so.
As part of the process of moving to Visual Studio 2019, we have dropped support for .NET 3.5 applications.

## How do i build Snoop?

Just open `Snoop.sln` with Visual Studio and build it.

Requirements:

- Visual Studio 2019 16.8 (including C++ payloads (x86/x64/ARM/ARM64))
  - You can import the `.vsconfig` file in the Visual Studio installer to let it install all required components
- .NET Core SDK 5.0.100

## Contributors

Over time contributions have been added by several people, most notably:

- [Bastian Schmidt](https://github.com/batzen), [batzendev.de](https://batzendev.de) (current maintainer)
- [Cory Plotts](https://github.com/cplotts), [cplotts.com](https://cplotts.com)
- [Dan Hanan](http://blogs.interknowlogy.com/author/danhanan/)
- [Andrei Kashcha](http://blog.yasiv.com/)
- [Maciek Rakowski](https://github.com/MaciekRakowski)
- [Bailey Ling](https://github.com/bling)