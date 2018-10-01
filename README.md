<h1>Snoop</h1>

<p>Snoop is the open source WPF spying utility created by <a href="https://github.com/peteblois">Pete Blois</a> when he was employed at Microsoft and now maintained by myself <a href="http://www.cplotts.com">Cory Plotts</a>. Over time contributions have been added by several people, most notably: <a href="http://blogs.interknowlogy.com/author/danhanan/">Dan Hanan</a>, <a href="http://blog.yasiv.com/">Andrei Kashcha</a>, <a href="https://github.com/MaciekRakowski">Maciek Rakowski</a>, <a href="https://github.com/bling">Bailey Ling</a>, and <a href="https://github.com/batzen">Bastian Schmidt</a>.</p>

<p>It allows you to spy/browse the visual tree of a running application (without the need for a debugger) ... and change properties ... amongst other things.</p>

<h2>Snoop 2.10.0</h2>

<p>The most recent version of Snoop, <a href="https://github.com/cplotts/snoopwpf/releases/tag/2.10.0">Snoop 2.10.0</a>, was released on September 19th, 2018. In 2.10.0 we finally got rid of support for Snooping WPF 3.5 applications. This allowed us to move the Snoop projects forward to Visual Studio 2017 ... which should make it much easier to work with Snoop's source code.</p>

<h2>Snoop 2.9.0</h2>

<p><a href="https://github.com/cplotts/snoopwpf/releases/tag/2.9.0">Snoop 2.9.0</a>, was released on July 27th, 2018. The big addition in 2.9.0 was the inclusion of the triggers tab which was a useful feature of another WPF spying utility called WPF Inspector (written by <a href="https://github.com/ChristianMoser">Christan Moser</a>). It was ported to Snoop by Bastian Schmidt.</p>

<h2>Documentation on How to Use Snoop</h2>

<p>Unfortunately there isn't any exhaustive documentation on how to use Snoop and there are plenty of hidden features. If someone is willing to work on this, please let me know. On the bright side, it is a pretty easy utility to use and learn. I have made three videos which should get most people quick started.</p>

<p>Here are the links to the current Snoop Tips &amp; Tricks: <a href="http://www.cplotts.com/2011/02/10/snoop-tips-tricks-1-ctrl-shift-mouse-over/">#1</a>, <a href="http://www.cplotts.com/2011/02/14/snoop-tips-tricks-2-snooping-transient-visuals/">#2</a>, <a href="http://www.cplotts.com/2012/05/31/snoop-tips-tricks-3-the-crosshairs/">#3</a>.</p>

<h2>Why Aren't My Apps Showing Up in the App Chooser?</h2>

<p>One question that comes up all the time is the situation where the application you are trying to Snoop, isn't appearing in the application chooser (i.e. the&#160; combo box that lists the processes you can Snoop). This is more than likely a situation where the application you are trying to Snoop is running elevated (as Administrator). In order to Snoop these applications, you will also need to run Snoop elevated (as Administrator).</p>

<h2>Why Can't I Snoop WPF 3.5 Applications?</h2>

<p>Well, you can! You will just need to use Snoop 2.9.0 and earlier, in order to do so. As part of the process of moving to Visual Studio 2017, we have dropped support for WPF 3.5 applications.</p>

<h2>Snoop's Gitter Room</h2>

<p>I recently created a <a href="https://gitter.im/snoopwpf/Lobby">Gitter room</a> for Snoop.</p>

<h2>How Do I Build Snoop?</h2>

<p>I just upgraded all the projects in Snoop so that you can build it with Visual Studio 2017. This means that I deleted all committed binaries. In other words, you should be able to open Snoop.sln and rebuild all and those binaries (like Snoop.exe, the managed injector dlls, and the managed injector launcher exes) will all get rebuilt.</p>

<p>Note that if you are going to run Snoop somewhere where you haven't built it ... you are likely going to need the Microsoft Visual C++ Redistributable(s) for Visual Studio 2017 ... for both x86 and x64 ... installed on that machine. See <a href="https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads">here</a> for some download links to the redists.

<h2>How Do I Build Earlier Versions of Snoop (2.9.0 and Earlier)?</h2>

<p>Most people will only need to load the SnoopOnly.sln solution file. This solution will build under most versions of Visual Studio, most notably Visual Studio 2017. The main Snoop project intentionally targets .NET 3.5 Client Profile because Snoop has, for years, supported Snooping both .NET 3.5 and .NET 4.0 applications. At some point in the future, we will change this and Snoop will only support Snooping .NET 4.0 (and forward) applications.</p>

<p>The full Snoop.sln solution file includes the projects for the managed injector and the managed injector launcher projects ... as well as the installer. Currently, Snoop can only fully be built (Snoop.sln) with Visual Studio 2010 and with the Windows Installer XML Toolset v3.5 installed. The WiX Toolset v3.5 is not easily available anymore. I have an Wix35.msi saved that I can happily give to anyone who wants to build the installer. <a href="https://gitter.im/snoopwpf/Lobby">Contact</a> me on Snoop's new Gitter community.</p>

<p>One other interesting piece of related info is that Setup.exe is a bootstrapper that installs the MSVC++ 2010 redestributables that then launches Snoop.msi. I don't have the source code for this bootstrapper as it was done by a friend of mine. Setup.exe is included in the source code.</p>

<p>I am also looking for someone who might want to help update the installer so that it uses a modern version of WiX ... or in general to own the installer part of Snoop as I have little interest in that. <a href="https://gitter.im/snoopwpf/Lobby">Contact</a> me if you are interested.</p>
