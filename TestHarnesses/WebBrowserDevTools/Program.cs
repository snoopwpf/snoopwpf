namespace WebBrowserDevTools;

using System;
using CefSharp;
using CefSharp.Wpf;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        //For Windows 7 and above, best to include relevant app.manifest entries as well
        Cef.EnableHighDPISupport();

        //Perform dependency check to make sure all relevant resources are in our output directory.
        Cef.Initialize(new CefSettings(), performDependencyCheck: true, browserProcessHandler: null);

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}