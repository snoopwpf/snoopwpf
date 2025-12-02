// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#if MCP_SDK
namespace Snoop.Infrastructure.MCP;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using ModelContextProtocol.Server;
using Snoop.Data.Tree;

/// <summary>
/// Provides access to Snoop's inspection context for MCP tools.
/// </summary>
public class SnoopContext
{
    public required Func<TreeItem?> GetCurrentSelection { get; init; }
    public required Func<TreeItem?> GetRootTreeItem { get; init; }
    public required Action<object?> SelectItem { get; init; }
}

/// <summary>
/// MCP tools for inspecting WPF applications through Snoop.
/// </summary>
[McpServerToolType]
public sealed class SnoopMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerTool("get_visual_tree"), Description("Get the visual tree structure of the currently inspected WPF application. Returns a hierarchical view of all UI elements.")]
    public static string GetVisualTree(
        SnoopContext context,
        [Description("Maximum depth to traverse (default: 10, max: 50)")] int maxDepth = 10,
        [Description("Include key properties for each element (default: false)")] bool includeProperties = false)
    {
        var root = context.GetRootTreeItem();
        if (root is null)
        {
            return JsonSerializer.Serialize(new { error = "No visual tree available" }, JsonOptions);
        }

        maxDepth = Math.Clamp(maxDepth, 1, 50);

        var tree = BuildTreeNode(root, 0, maxDepth, includeProperties, "");
        return JsonSerializer.Serialize(new { tree }, JsonOptions);
    }

    [McpServerTool("get_selected_element"), Description("Get detailed information about the currently selected element in the Snoop inspector, including its properties.")]
    public static string GetSelectedElement(SnoopContext context)
    {
        var selection = context.GetCurrentSelection();
        if (selection is null)
        {
            return JsonSerializer.Serialize(new { error = "No element selected" }, JsonOptions);
        }

        var result = new Dictionary<string, object?>
        {
            ["type"] = selection.TargetType.Name,
            ["fullTypeName"] = selection.TargetType.FullName,
            ["name"] = selection.Name,
            ["displayName"] = selection.DisplayName,
            ["depth"] = selection.Depth,
            ["childCount"] = selection.Children.Count,
            ["hasBindingError"] = selection.HasBindingError
        };

        if (selection.Target is DependencyObject depObj)
        {
            result["properties"] = GetAllProperties(depObj, null);
        }

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool("get_element_properties"), Description("Get all properties of a specific element identified by its path in the visual tree.")]
    public static string GetElementProperties(
        SnoopContext context,
        [Description("Path to the element (e.g., '0/2/1' for root's child 0, then child 2, then child 1)")] string path,
        [Description("Optional filter for property names (case-insensitive substring match)")] string? filter = null)
    {
        var element = FindElementByPath(context, path);
        if (element is null)
        {
            return JsonSerializer.Serialize(new { error = $"Element not found at path: {path}" }, JsonOptions);
        }

        if (element.Target is not DependencyObject depObj)
        {
            return JsonSerializer.Serialize(new { error = "Element is not a DependencyObject" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            type = element.TargetType.Name,
            name = element.Name,
            path,
            properties = GetAllProperties(depObj, filter)
        }, JsonOptions);
    }

    [McpServerTool("select_element"), Description("Select an element in the visual tree by its path, making it the current selection in Snoop.")]
    public static string SelectElement(
        SnoopContext context,
        [Description("Path to the element (e.g., '0/2/1')")] string path)
    {
        var element = FindElementByPath(context, path);
        if (element is null)
        {
            return JsonSerializer.Serialize(new { error = $"Element not found at path: {path}" }, JsonOptions);
        }

        context.SelectItem(element.Target);

        return JsonSerializer.Serialize(new
        {
            success = true,
            selected = new
            {
                type = element.TargetType.Name,
                name = element.Name,
                path
            }
        }, JsonOptions);
    }

    [McpServerTool("find_elements"), Description("Find elements in the visual tree by type name or element name.")]
    public static string FindElements(
        SnoopContext context,
        [Description("Type name to search for (e.g., 'Button', 'TextBox')")] string? typeName = null,
        [Description("Element x:Name to search for")] string? elementName = null,
        [Description("Maximum number of results to return (default: 20)")] int maxResults = 20)
    {
        if (string.IsNullOrEmpty(typeName) && string.IsNullOrEmpty(elementName))
        {
            return JsonSerializer.Serialize(new { error = "Either typeName or elementName must be provided" }, JsonOptions);
        }

        var root = context.GetRootTreeItem();
        if (root is null)
        {
            return JsonSerializer.Serialize(new { error = "No visual tree available" }, JsonOptions);
        }

        var results = new List<object>();
        SearchTree(root, typeName, elementName, "", results, maxResults);

        return JsonSerializer.Serialize(new
        {
            count = results.Count,
            elements = results
        }, JsonOptions);
    }

    [McpServerTool("get_bindings"), Description("Get all data bindings on the currently selected element, including binding errors.")]
    public static string GetBindings(SnoopContext context)
    {
        var selection = context.GetCurrentSelection();
        if (selection is null)
        {
            return JsonSerializer.Serialize(new { error = "No element selected" }, JsonOptions);
        }

        if (selection.Target is not DependencyObject depObj)
        {
            return JsonSerializer.Serialize(new { error = "Selected element is not a DependencyObject" }, JsonOptions);
        }

        var bindings = new List<object>();
        var localValueEnumerator = depObj.GetLocalValueEnumerator();

        while (localValueEnumerator.MoveNext())
        {
            var entry = localValueEnumerator.Current;
            var binding = System.Windows.Data.BindingOperations.GetBindingBase(depObj, entry.Property);

            if (binding is not null)
            {
                var bindingInfo = new Dictionary<string, object?>
                {
                    ["property"] = entry.Property.Name,
                    ["propertyType"] = entry.Property.PropertyType.Name
                };

                if (binding is System.Windows.Data.Binding b)
                {
                    bindingInfo["path"] = b.Path?.Path;
                    bindingInfo["mode"] = b.Mode.ToString();
                    bindingInfo["source"] = b.Source?.GetType().Name;
                    bindingInfo["elementName"] = b.ElementName;
                    bindingInfo["relativeSource"] = b.RelativeSource?.Mode.ToString();
                }

                var expression = System.Windows.Data.BindingOperations.GetBindingExpression(depObj, entry.Property);
                if (expression is not null)
                {
                    bindingInfo["hasError"] = expression.HasError;
                    if (expression.HasError)
                    {
                        bindingInfo["errorStatus"] = expression.Status.ToString();
                    }
                }

                bindings.Add(bindingInfo);
            }
        }

        return JsonSerializer.Serialize(new
        {
            elementType = selection.TargetType.Name,
            elementName = selection.Name,
            bindingCount = bindings.Count,
            bindings
        }, JsonOptions);
    }

    #region Helper Methods

    private static object BuildTreeNode(TreeItem item, int currentDepth, int maxDepth, bool includeProperties, string path)
    {
        var node = new Dictionary<string, object?>
        {
            ["type"] = item.TargetType.Name,
            ["name"] = item.Name,
            ["path"] = path,
            ["depth"] = item.Depth
        };

        if (includeProperties && item.Target is DependencyObject depObj)
        {
            node["keyProperties"] = GetKeyProperties(depObj);
        }

        if (currentDepth < maxDepth && item.Children.Count > 0)
        {
            var children = new List<object>();
            for (var i = 0; i < item.Children.Count; i++)
            {
                var childPath = string.IsNullOrEmpty(path) ? i.ToString() : $"{path}/{i}";
                children.Add(BuildTreeNode(item.Children[i], currentDepth + 1, maxDepth, includeProperties, childPath));
            }

            node["children"] = children;
            node["childCount"] = item.Children.Count;
        }
        else if (item.Children.Count > 0)
        {
            node["childCount"] = item.Children.Count;
            node["truncated"] = true;
        }

        return node;
    }

    private static Dictionary<string, object?> GetKeyProperties(DependencyObject depObj)
    {
        var props = new Dictionary<string, object?>();

        if (depObj is FrameworkElement fe)
        {
            if (!string.IsNullOrEmpty(fe.Name))
            {
                props["Name"] = fe.Name;
            }

            props["Width"] = double.IsNaN(fe.Width) ? "Auto" : fe.Width;
            props["Height"] = double.IsNaN(fe.Height) ? "Auto" : fe.Height;
            props["ActualWidth"] = fe.ActualWidth;
            props["ActualHeight"] = fe.ActualHeight;
            props["IsEnabled"] = fe.IsEnabled;
            props["Visibility"] = fe.Visibility.ToString();

            if (fe.DataContext is not null)
            {
                props["DataContextType"] = fe.DataContext.GetType().Name;
            }
        }

        if (depObj is System.Windows.Controls.ContentControl cc && cc.Content is not null)
        {
            props["Content"] = cc.Content is string s ? s : cc.Content.GetType().Name;
        }

        if (depObj is System.Windows.Controls.TextBlock tb)
        {
            props["Text"] = tb.Text?.Length > 100 ? tb.Text[..100] + "..." : tb.Text;
        }

        if (depObj is System.Windows.Controls.TextBox textBox)
        {
            props["Text"] = textBox.Text?.Length > 100 ? textBox.Text[..100] + "..." : textBox.Text;
        }

        return props;
    }

    private static List<Dictionary<string, object?>> GetAllProperties(DependencyObject depObj, string? filter)
    {
        var properties = new List<Dictionary<string, object?>>();

        var dpDescriptors = System.ComponentModel.TypeDescriptor.GetProperties(depObj);

        foreach (System.ComponentModel.PropertyDescriptor? pd in dpDescriptors)
        {
            if (pd is null)
            {
                continue;
            }

            if (filter is not null && !pd.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var value = pd.GetValue(depObj);
                var valueStr = FormatValue(value);

                properties.Add(new Dictionary<string, object?>
                {
                    ["name"] = pd.Name,
                    ["value"] = valueStr,
                    ["type"] = pd.PropertyType.Name,
                    ["isReadOnly"] = pd.IsReadOnly
                });
            }
            catch
            {
                // Skip properties that throw
            }
        }

        return properties.OrderBy(p => p["name"]?.ToString()).ToList();
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string s)
        {
            return s.Length > 200 ? s[..200] + "..." : s;
        }

        if (value is Brush brush)
        {
            return brush.ToString();
        }

        if (value is Transform transform)
        {
            return transform.ToString();
        }

        var type = value.GetType();

        if (type.IsPrimitive || type == typeof(decimal) || type.IsEnum)
        {
            return value.ToString() ?? "null";
        }

        return $"[{type.Name}]";
    }

    private static TreeItem? FindElementByPath(SnoopContext context, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return context.GetRootTreeItem();
        }

        var root = context.GetRootTreeItem();
        if (root is null)
        {
            return null;
        }

        var indices = path.Split('/').Select(int.Parse).ToArray();
        var current = root;

        foreach (var index in indices)
        {
            if (index < 0 || index >= current.Children.Count)
            {
                return null;
            }

            current = current.Children[index];
        }

        return current;
    }

    private static void SearchTree(TreeItem item, string? typeName, string? elementName, string path, List<object> results, int maxResults)
    {
        if (results.Count >= maxResults)
        {
            return;
        }

        var matches = true;

        if (!string.IsNullOrEmpty(typeName))
        {
            matches = item.TargetType.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase);
        }

        if (matches && !string.IsNullOrEmpty(elementName))
        {
            matches = item.Name.Contains(elementName, StringComparison.OrdinalIgnoreCase);
        }

        if (matches && (!string.IsNullOrEmpty(typeName) || !string.IsNullOrEmpty(elementName)))
        {
            results.Add(new
            {
                type = item.TargetType.Name,
                name = item.Name,
                path,
                depth = item.Depth
            });
        }

        for (var i = 0; i < item.Children.Count && results.Count < maxResults; i++)
        {
            var childPath = string.IsNullOrEmpty(path) ? i.ToString() : $"{path}/{i}";
            SearchTree(item.Children[i], typeName, elementName, childPath, results, maxResults);
        }
    }

    #endregion
}
#endif
