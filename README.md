# Snoop
Snoop is the open source WPF spying utility created by [Pete Blois](https://github.com/peteblois) when he was employed at Microsoft.

It allows you to spy/browse the visual tree of a running application (without the need for a debugger) and change properties, view triggers, set breakpoints on property changes and many more things.

[![Build status](https://img.shields.io/appveyor/ci/batzen/snoopwpf.svg?style=flat-square&&label=master)](https://ci.appveyor.com/project/batzen/snoopwpf/branch/master)
[![chocolatey version](http://img.shields.io/chocolatey/v/snoop.svg?style=flat-square)](https://chocolatey.org/packages/snoop)
[![chocolatey download count](http://img.shields.io/chocolatey/dt/snoop.svg?style=flat-square)](https://chocolatey.org/packages/snoop)

## Contact
- [![Join the chat at https://gitter.im/snoopwpf/Lobby](https://img.shields.io/badge/GITTER-join%20chat-green.svg?style=flat-square)](https://gitter.im/snoopwpf/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
- [![Twitter](https://img.shields.io/badge/twitter-%40batzendev-blue.svg?style=flat-square)](https://twitter.com/batzendev)

## Where can i download snoop?/How can i install snoop?
- [chocolatey](https://chocolatey.org/packages/snoop)
- [github releases](https://github.com/snoopwpf/snoopwpf/releases)
- [appveyor](https://ci.appveyor.com/project/batzen/snoopwpf) for the latest preview versions (built on every code change)
- Please note that you need at least .NET Framework 4.0 and the Microsoft Visual C++ Redistributable(s) 2017 to run snoop

# Versions
## [2.11.0](releases/tag/2.11.0)
You can read the [changelog](Changelog.md) for details on what changed in this version.

Highlights:
- Support for multiple app domains
- Auto elevation to enable spying of elevated processes without running Snoop as administrator
- Persistent settings for various settings
- Improved error dialog and issue reporting
- Rewritten window finder

## [2.10.0](releases/tag/2.10.0)
Was released on September 19th, 2018. 
In this version we finally got rid of support for Snooping WPF 3.5 applications. 
This allowed us to move the Snoop projects forward to Visual Studio 2017 which should make it much easier to work with Snoop's source code.

## [2.9.0](releases/tag/2.9.0)
Was released on July 27th, 2018. 
The big addition in this version was the inclusion of the triggers tab which was a useful feature of another WPF spying utility called WPF Inspector (written by [Christan Moser](https://github.com/ChristianMoser)). 
It was ported to Snoop by Bastian Schmidt.

## Documentation on how to Use Snoop
Unfortunately there isn't any exhaustive documentation on how to use Snoop and there are plenty of hidden features. If someone is willing to work on this, please let me know. On the bright side, it is a pretty easy utility to use and learn. I have made three videos which should get most people quick started.

Here are the links to the current Snoop Tips & Tricks: 
- http://www.cplotts.com/2011/02/10/snoop-tips-tricks-1-ctrl-shift-mouse-over
- http://www.cplotts.com/2011/02/14/snoop-tips-tricks-2-snooping-transient-visuals
- http://www.cplotts.com/2012/05/31/snoop-tips-tricks-3-the-crosshairs

## Why Can't I Snoop WPF 3.5 Applications?
Well, you can! You will just need to use Snoop 2.9.0 and earlier, in order to do so.
As part of the process of moving to Visual Studio 2017, we have dropped support for WPF 3.5 applications.

## How do i build Snoop?
Just open `Snoop.sln` with Visual Studio 2017 (or later) and build it.
Please note that you need the Visual Studio 2017 C++ payload and in case you are using a later version you also need the VC++ 141 payload.

Note that if you are going to run Snoop somewhere where you haven't built it you are likely going to need the Microsoft Visual C++ Redistributable(s) for Visual Studio 2017 for both x86 and x64 installed on that machine. See [here](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads) for some download links to the redists.

## Contributors
Over time contributions have been added by several people, most notably: 
- [Bastian Schmidt](https://github.com/batzen), [batzendev.de](https://batzendev.de) (current maintainer)
- [Cory Plotts](https://github.com/cplotts), [cplotts.com](https://cplotts.com)
- [Dan Hanan](http://blogs.interknowlogy.com/author/danhanan/)
- [Andrei Kashcha](http://blog.yasiv.com/)
- [Maciek Rakowski](https://github.com/MaciekRakowski)
- [Bailey Ling](https://github.com/bling)