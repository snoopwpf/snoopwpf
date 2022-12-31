namespace Snoop.Data.Tree;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Snoop.Infrastructure;

public class WebBrowserTreeItem : DependencyObjectTreeItem
{
    public WebBrowserTreeItem(DependencyObject target, TreeItem? parent, TreeService treeService)
        : base(target, parent, treeService)
    {
    }

    protected override ObservableCollection<MenuItem> CreateMenuItems()
    {
        var items = base.CreateMenuItems();

        items.Add(new MenuItem { Header = "Show dev tools", Command = new RelayCommand(_ => this.ShowDevTools()) });

        return items;
    }

    private void ShowDevTools()
    {
        var dependencyObject = this.DependencyObject;
        var dependencyObjectType = dependencyObject.GetType();
        var interfaces = dependencyObjectType.GetInterfaces();

        if (IsWebView2(dependencyObject))
        {
            var coreWebView2 = dependencyObjectType.GetProperty("CoreWebView2")?.GetValue(dependencyObject, null);

            coreWebView2?.GetType().GetMethod("OpenDevToolsWindow")?.Invoke(coreWebView2, null);
        } // CefSharp browser control
        else if (interfaces.Any(x => x.FullName is "CefSharp.IBrowserHost"))
        {
            dependencyObjectType.GetMethod("ShowDevTools")?.Invoke(dependencyObject, new object?[] { null, 0, 0 });
        } // CefSharp browser control
        else if (interfaces.Any(x => x.FullName is "CefSharp.IBrowser"))
        {
            var host = dependencyObjectType.GetMethod("GetHost")?.Invoke(dependencyObject, null);

            host?.GetType().GetMethod("ShowDevTools")?.Invoke(host, new object?[] { null, 0, 0 });
        } // CefSharp browser control
        else if (interfaces.Any(x => x.FullName is "CefSharp.IChromiumWebBrowserBase"))
        {
            var browserCore = dependencyObjectType.GetProperty("BrowserCore")?.GetValue(dependencyObject);

            var host = browserCore?.GetType().GetMethod("GetHost")?.Invoke(browserCore, null);

            host?.GetType().GetMethod("ShowDevTools")?.Invoke(host, new object?[] { null, 0, 0 });
        }
    }

    public static bool IsWebBrowserWithDevToolsSupport(DependencyObject dependencyObject)
    {
        var dependencyObjectType = dependencyObject.GetType();

        return dependencyObjectType.GetInterfaces().Any(x =>
            x.FullName is "CefSharp.IBrowserHost"
            or "CefSharp.IBrowser"
            or "CefSharp.IChromiumWebBrowserBase")
            || IsWebView2(dependencyObject);
    }

    private static bool IsWebView2(DependencyObject dependencyObject)
    {
        if (dependencyObject is not HwndHost)
        {
            return false;
        }

        var currentType = dependencyObject.GetType();
        while (currentType is not null)
        {
            if (currentType.Name is "WebView2")
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }
}