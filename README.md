# Snoop

Snoop is an open source WPF spying utility originally created by [Pete Blois](https://github.com/peteblois) and is currently maintained by [Bastian Schmidt](https://github.com/batzen).

It allows you to spy/browse the visual, logical and automation tree of any running WPF application (without the need for a debugger).  
You can change property values, view triggers, set breakpoints on property changes and many more things.

[![Build status for master branch](https://img.shields.io/appveyor/ci/batzen/snoopwpf/master?style=flat-square&&label=master)](https://ci.appveyor.com/project/batzen/snoopwpf/branch/master)
[![Build status for develop branch](https://img.shields.io/appveyor/ci/batzen/snoopwpf/develop?style=flat-square&&label=develop)](https://ci.appveyor.com/project/batzen/snoopwpf/branch/develop)
[![Chocolatey version](http://img.shields.io/chocolatey/v/snoop.svg?style=flat-square)](https://chocolatey.org/packages/snoop)
[![Chocolatey download count](http://img.shields.io/chocolatey/dt/snoop.svg?style=flat-square)](https://chocolatey.org/packages/snoop)

## Contact

- [![Join the chat at https://gitter.im/snoopwpf/Lobby](https://img.shields.io/badge/GITTER-join%20chat-green.svg?style=flat-square)](https://gitter.im/snoopwpf/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
- [![Twitter](https://img.shields.io/badge/twitter-%40batzendev-blue.svg?style=flat-square)](https://twitter.com/batzendev)

## Where can i download Snoop?/How can i install Snoop?

- [Chocolatey](https://chocolatey.org/packages/snoop) for stable and some preview versions
- [GitHub releases](https://github.com/snoopwpf/snoopwpf/releases) for stable versions
- [AppVeyor](https://ci.appveyor.com/project/batzen/snoopwpf/build/artifacts) for the latest preview versions (built on every code change)
- You need at least .NET Framework 4.6.2 to run Snoop

## Supported .NET versions

- .NET Framework >= 4.6.2
- .NET >= 6
  - Tested with 6, 7, 8, 9 and 10. Future versions might just work.
  - **Restrictions:** Self-Contained single file applications are not supported as there is no reliable way to get a handle to the .NET runtime

## AI Assistant Integration (MCP Server)

Snoop now includes an **MCP (Model Context Protocol) server** that allows AI assistants like Claude to interact with and inspect your WPF applications in real-time.

### How to Use

1. Open Snoop and attach to a WPF application
2. Click the "MCP Server" button in the menu
3. Start the server in the MCP Server window
4. Configure your AI assistant (e.g., Claude Desktop) to connect to the server endpoint

### Available MCP Tools

The MCP server exposes 7 tools for AI assistants:

- `get_visual_tree` - Get hierarchical structure of UI elements
- `get_selected_element` - Get details of currently selected element
- `get_element_properties` - Query properties of a specific element
- `select_element` - Programmatically select elements in Snoop
- `find_elements` - Search for elements by type or name
- `get_bindings` - Analyze data bindings and binding errors
- `get_element_preview` - Capture visual screenshots of elements

### Use Cases

- Debug WPF layout issues with AI assistance
- Automated UI inspection and testing
- Find binding errors quickly
- Performance bottleneck analysis
- Generate detailed bug reports with screenshots
- Learn WPF patterns from real applications

### Requirements

- .NET 8.0+ for official MCP SDK support
- .NET 6.0+ for fallback implementation
- Works with both .NET Framework and .NET applications

The MCP server runs on `localhost:47700-47799` using Server-Sent Events (SSE) transport.

## Changelog

You can read the [changelog](Changelog.md) for details on what changed in each version.

## Documentation on how to use Snoop

Unfortunately there isn't any exhaustive documentation on how to use Snoop and there are plenty of hidden features. If someone is willing to work on this, please let me know. On the bright side, it is a pretty easy utility to use and learn. I have made three videos which should get most people quick started.

Here are the links to the current Snoop Tips & Tricks:

- https://www.youtube.com/watch?v=n8EdRR0Tc1k
- https://www.youtube.com/watch?v=98UEVCQHmVA
- https://www.youtube.com/watch?v=frXAgGzZnrU

## Why can't I snoop my application?

Well, you can! You will just need to use an earlier version of Snoop, in order to do so.  
The minimum versions are:

| Snoop | .NET Framework | .NET |
|-------|----------------|------|
| 3.0   | 4.0            | 3.0  |
| 4.0   | 4.5.1          | 3.0  |
| 5.0   | 4.5.2          | 3.1  |
| 6.0   | 4.6.2          | 6.0  |

## How do i build Snoop?

Just open `Snoop.sln` with Visual Studio and build it.

Requirements:

- Visual Studio 2022 or later
  - C++ payloads (x86/x64 and optionally ARM/ARM64)
  - You can import the [.vsconfig](.vsconfig) file in the Visual Studio installer to let it install all required components
- .NET SDK 8.0.401 or later

## Contributors

Over time contributions have been added by several people, most notably:

- [Bastian Schmidt](https://github.com/batzen), [batzen.dev](https://batzen.dev) (current maintainer)
- [Cory Plotts](https://github.com/cplotts)
- [Dan Hanan](http://blogs.interknowlogy.com/author/danhanan/)
- [Andrei Kashcha](http://blog.yasiv.com/)
- [Maciek Rakowski](https://github.com/MaciekRakowski)
- [Bailey Ling](https://github.com/bling)

## Code Signing

Snoop uses free code signing provided by [SignPath.io](https://signpath.io?utm_source=foundation&utm_medium=github&utm_campaign=snoopwpf) and a free code signing certificate by the [SignPath Foundation](https://signpath.org?utm_source=foundation&utm_medium=github&utm_campaign=snoopwpf)
