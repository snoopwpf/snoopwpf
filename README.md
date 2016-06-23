<h1>Snoop</h1>

<p>Snoop is the open source WPF spying utility created by Pete Blois and now maintained by Team Snoop (<a href="http://www.cplotts.com">Cory Plotts</a>, <a href="http://blogs.interknowlogy.com/author/danhanan/">Dan Hanan</a>, <a href="http://blog.yasiv.com/">Andrei Kashcha</a>, Maciek Rakowski, and Jason Jibben).</p>

<p>It allows you to spy/browse the visual tree of a running application (without the need for a debugger) ... and change properties ... amongst other things.</p>

<h2>Snoop 2.8.0</h2>

<p>The most recent version of Snoop, <a href="http://snoopwpf.codeplex.com/releases/view/87261">Snoop 2.8.0</a>, was released on October 4th, 2012. Go to the download page on <a href="http://snoopwpf.codeplex.com/releases/view/87261">CodePlex</a> to download it. Most notably, with this release, Bailey Ling has added a PowerShell scripting tab.</p>

<h2>Git</h2>

<p>The Snoop repository has been converted to Git and is now being hosted in two public repositories (which will be kept in sync), the one at CodePlex (<a href="http://snoopwpf.codeplex.com/">http://snoopwpf.codeplex.com/</a>) and the one at GitHub (<a href="https://github.com/cplotts/snoopwpf">https://github.com/cplotts/snoopwpf</a>). See the 2.7.1 <a href="http://snoopwpf.codeplex.com/releases/view/73187">release notes</a> for more info.</p>

<h2>Documentation on How to Use Snoop</h2>

<p>I am finally getting to business on the <a href="http://snoopwpf.codeplex.com/documentation">Documentation</a> area on CodePlex. It will be a work in progress for a bit. Please forgive the mess.</p>

<p>Here are the links to the current Snoop Tips &amp; Tricks: <a href="http://www.cplotts.com/2011/02/10/snoop-tips-tricks-1-ctrl-shift-mouse-over/">#1</a>, <a href="http://www.cplotts.com/2011/02/14/snoop-tips-tricks-2-snooping-transient-visuals/">#2</a>, <a href="http://www.cplotts.com/2012/05/31/snoop-tips-tricks-3-the-crosshairs/">#3</a>.</p>

<p>Also, don't forget about the documentation on Pete Blois' Snoop <a href="http://blois.us/Snoop">page</a>. It is still useful ... but hopefully will be less so once I finish my efforts.</p>

<h2>Why Aren't My Apps Showing Up in the App Chooser?</h2>

<p>One question that comes up all the time is the situation where the application you are trying to Snoop, isn't appearing in the application chooser (i.e. the&#160; combo box that lists the processes you can Snoop). This is more than likely a situation where the application you are trying to Snoop is running elevated (as Administrator). In order to Snoop these applications, you will also need to run Snoop elevated (as Administrator).</p>

<h2>Silverlight Support</h2>

<p>Snoop is not currently able to spy Silverlight applications (maybe some day). In the meantime, if you want to do that, I would point you to Koen Zwikstra's awesome utility, <a href="http://firstfloorsoftware.com/silverlightspy/">Silverlight Spy</a>.</p>