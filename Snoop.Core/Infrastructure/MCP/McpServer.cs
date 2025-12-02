// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure.MCP;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using JetBrains.Annotations;
using Snoop.Data.Tree;

#if MCP_SDK
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
#else
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
#endif

/// <summary>
/// MCP (Model Context Protocol) server for exposing Snoop inspection capabilities to AI assistants.
/// Uses HTTP with Server-Sent Events (SSE) for communication.
/// </summary>
public sealed class McpServer : INotifyPropertyChanged, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly Func<TreeItem?> getCurrentSelection;
    private readonly Func<TreeItem?> getRootTreeItem;
    private readonly Action<object?> selectItem;
    private int port;
    private bool isRunning;
    private CancellationTokenSource? cancellationTokenSource;

#if MCP_SDK
    private WebApplication? webApplication;
#else
    private HttpListener? httpListener;
    private string? sessionId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
#endif

    public McpServer(
        Dispatcher dispatcher,
        Func<TreeItem?> getCurrentSelection,
        Func<TreeItem?> getRootTreeItem,
        Action<object?> selectItem)
    {
        this.dispatcher = dispatcher;
        this.getCurrentSelection = getCurrentSelection;
        this.getRootTreeItem = getRootTreeItem;
        this.selectItem = selectItem;
    }

    public bool IsRunning
    {
        get => this.isRunning;
        private set
        {
            if (this.isRunning == value)
            {
                return;
            }

            this.isRunning = value;
            this.OnPropertyChanged();
        }
    }

    public int Port
    {
        get => this.port;
        private set
        {
            if (this.port == value)
            {
                return;
            }

            this.port = value;
            this.OnPropertyChanged();
            this.OnPropertyChanged(nameof(this.ConnectionUrl));
            this.OnPropertyChanged(nameof(this.SseEndpoint));
        }
    }

    public string ConnectionUrl => $"http://localhost:{this.Port}";

    public string SseEndpoint => $"{this.ConnectionUrl}/sse";

#if MCP_SDK
    public async Task<bool> StartAsync(int preferredPort = 0)
    {
        if (this.IsRunning)
        {
            return true;
        }

        this.cancellationTokenSource = new CancellationTokenSource();

        // Find an available port
        var portToUse = preferredPort > 0 ? preferredPort : FindAvailablePort();
        if (portToUse == 0)
        {
            return false;
        }

        try
        {
            // Create the SnoopContext that will be injected into the tools
            var snoopContext = new SnoopContext
            {
                GetCurrentSelection = () => this.dispatcher.Invoke(this.getCurrentSelection),
                GetRootTreeItem = () => this.dispatcher.Invoke(this.getRootTreeItem),
                SelectItem = target => this.dispatcher.Invoke(() => this.selectItem(target))
            };

            var builder = WebApplication.CreateSlimBuilder();

            // Configure logging to reduce noise
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Configure the server URL
            builder.WebHost.UseUrls($"http://localhost:{portToUse}");

            // Add MCP server with the Snoop tools
            builder.Services.AddSingleton(snoopContext);
            builder.Services
                .AddMcpServer()
                .WithHttpTransport()
                .WithTools<SnoopMcpTools>();

            this.webApplication = builder.Build();

            // Map the MCP endpoints (/sse and /messages)
            this.webApplication.MapMcp();

            // Start the server
            _ = Task.Run(async () =>
            {
                try
                {
                    await this.webApplication.RunAsync(this.cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MCP Server error: {ex.Message}");
                }
            });

            // Give the server a moment to start
            await Task.Delay(100);

            this.Port = portToUse;
            this.IsRunning = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start MCP server: {ex.Message}");
            return false;
        }
    }

    private static int FindAvailablePort()
    {
        // Try ports in the range 47700-47799
        foreach (var testPort in Enumerable.Range(47700, 100))
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, testPort);
                listener.Start();
                listener.Stop();
                return testPort;
            }
            catch (SocketException)
            {
                // Port in use, try next
            }
        }

        return 0;
    }

    public async void Stop()
    {
        if (!this.IsRunning)
        {
            return;
        }

        try
        {
            this.cancellationTokenSource?.Cancel();

            if (this.webApplication is not null)
            {
                await this.webApplication.StopAsync();
                await this.webApplication.DisposeAsync();
                this.webApplication = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping MCP server: {ex.Message}");
        }

        this.IsRunning = false;
        this.Port = 0;
    }

    public void Dispose()
    {
        this.Stop();
        this.cancellationTokenSource?.Dispose();
    }

#else
    // Fallback implementation for .NET Framework and .NET 6
    public async Task<bool> StartAsync(int preferredPort = 0)
    {
        if (this.IsRunning)
        {
            return true;
        }

        this.cancellationTokenSource = new CancellationTokenSource();
        this.sessionId = Guid.NewGuid().ToString("N")[..8];

        // Try to find an available port
        var portsToTry = preferredPort > 0
            ? new[] { preferredPort }
            : Enumerable.Range(47700, 100).ToArray();

        foreach (var testPort in portsToTry)
        {
            try
            {
                this.httpListener = new HttpListener();
                this.httpListener.Prefixes.Add($"http://localhost:{testPort}/");
                this.httpListener.Start();
                this.Port = testPort;
                this.IsRunning = true;

                // Start listening for requests
                _ = Task.Run(() => this.ListenAsync(this.cancellationTokenSource.Token));

                return true;
            }
            catch (HttpListenerException)
            {
                this.httpListener?.Close();
                this.httpListener = null;
            }
        }

        return false;
    }

    public void Stop()
    {
        if (!this.IsRunning)
        {
            return;
        }

        this.cancellationTokenSource?.Cancel();
        this.httpListener?.Stop();
        this.httpListener?.Close();
        this.httpListener = null;
        this.IsRunning = false;
        this.Port = 0;
    }

    public void Dispose()
    {
        this.Stop();
        this.cancellationTokenSource?.Dispose();
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && this.httpListener?.IsListening == true)
        {
            try
            {
                var context = await this.httpListener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => this.HandleRequestAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MCP Server error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            // Add CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            var path = request.Url?.AbsolutePath ?? "/";

            switch (path)
            {
                case "/sse":
                    await this.HandleSseConnectionAsync(context, cancellationToken);
                    break;

                case "/message":
                case "/messages":
                    await this.HandleMessageAsync(context);
                    break;

                default:
                    await this.SendJsonResponseAsync(response, new { error = "Not found" }, 404);
                    break;
            }
        }
        catch (Exception ex)
        {
            try
            {
                await this.SendJsonResponseAsync(response, new { error = ex.Message }, 500);
            }
            catch
            {
                // Ignore errors when sending error response
            }
        }
    }

    private async Task HandleSseConnectionAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;
        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

        using var writer = new StreamWriter(response.OutputStream, Encoding.UTF8, leaveOpen: true);

        // Send the endpoint event with the messages URL
        var endpointEvent = new
        {
            endpoint = $"{this.ConnectionUrl}/messages?sessionId={this.sessionId}"
        };
        await writer.WriteAsync($"event: endpoint\ndata: {JsonSerializer.Serialize(endpointEvent, JsonOptions)}\n\n");
        await writer.FlushAsync();

        // Keep the connection alive
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken); // Send ping every 15 seconds
                await writer.WriteAsync(": ping\n\n");
                await writer.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when server stops
        }
        catch (IOException)
        {
            // Client disconnected
        }
    }

    private async Task HandleMessageAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.HttpMethod != "POST")
        {
            await this.SendJsonResponseAsync(response, new { error = "Method not allowed" }, 405);
            return;
        }

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        try
        {
            var jsonDoc = JsonDocument.Parse(body);
            var root = jsonDoc.RootElement;

            var method = root.GetProperty("method").GetString();
            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetRawText() : null;

            object? result = method switch
            {
                "initialize" => this.HandleInitialize(root),
                "tools/list" => this.HandleToolsList(),
                "tools/call" => await this.HandleToolCallAsync(root),
                "notifications/initialized" => null, // Acknowledge
                "ping" => new { },
                _ => throw new InvalidOperationException($"Unknown method: {method}")
            };

            if (id is not null)
            {
                await this.SendJsonResponseAsync(response, new
                {
                    jsonrpc = "2.0",
                    id = JsonSerializer.Deserialize<object>(id),
                    result
                });
            }
            else
            {
                await this.SendJsonResponseAsync(response, new { success = true });
            }
        }
        catch (JsonException ex)
        {
            await this.SendJsonResponseAsync(response, new
            {
                jsonrpc = "2.0",
                error = new { code = -32700, message = $"Parse error: {ex.Message}" }
            }, 400);
        }
        catch (Exception ex)
        {
            await this.SendJsonResponseAsync(response, new
            {
                jsonrpc = "2.0",
                error = new { code = -32603, message = ex.Message }
            }, 500);
        }
    }

    private object HandleInitialize(JsonElement root)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = "snoop-wpf",
                version = typeof(McpServer).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            }
        };
    }

    private object HandleToolsList()
    {
        return new
        {
            tools = new[]
            {
                new
                {
                    name = "get_visual_tree",
                    description = "Get the visual tree structure of the currently inspected WPF application. Returns a hierarchical view of all UI elements.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            maxDepth = new
                            {
                                type = "integer",
                                description = "Maximum depth to traverse (default: 10, max: 50)"
                            },
                            includeProperties = new
                            {
                                type = "boolean",
                                description = "Include key properties for each element (default: false)"
                            }
                        }
                    }
                },
                new
                {
                    name = "get_selected_element",
                    description = "Get detailed information about the currently selected element in the Snoop inspector, including its properties.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new
                {
                    name = "get_element_properties",
                    description = "Get all properties of a specific element identified by its path in the visual tree.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            path = new
                            {
                                type = "string",
                                description = "Path to the element (e.g., '0/2/1' for root's child 0, then child 2, then child 1)"
                            },
                            filter = new
                            {
                                type = "string",
                                description = "Optional filter for property names (case-insensitive substring match)"
                            }
                        },
                        required = new[] { "path" }
                    }
                },
                new
                {
                    name = "select_element",
                    description = "Select an element in the visual tree by its path, making it the current selection in Snoop.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            path = new
                            {
                                type = "string",
                                description = "Path to the element (e.g., '0/2/1')"
                            }
                        },
                        required = new[] { "path" }
                    }
                },
                new
                {
                    name = "find_elements",
                    description = "Find elements in the visual tree by type name or element name.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            typeName = new
                            {
                                type = "string",
                                description = "Type name to search for (e.g., 'Button', 'TextBox')"
                            },
                            elementName = new
                            {
                                type = "string",
                                description = "Element x:Name to search for"
                            },
                            maxResults = new
                            {
                                type = "integer",
                                description = "Maximum number of results to return (default: 20)"
                            }
                        }
                    }
                },
                new
                {
                    name = "get_bindings",
                    description = "Get all data bindings on the currently selected element, including binding errors.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            }
        };
    }

    private async Task<object> HandleToolCallAsync(JsonElement root)
    {
        var paramsElement = root.GetProperty("params");
        var toolName = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var args) ? args : default;

        return await this.dispatcher.InvokeAsync(() =>
        {
            try
            {
                var result = toolName switch
                {
                    "get_visual_tree" => this.ExecuteGetVisualTree(arguments),
                    "get_selected_element" => this.ExecuteGetSelectedElement(),
                    "get_element_properties" => this.ExecuteGetElementProperties(arguments),
                    "select_element" => this.ExecuteSelectElement(arguments),
                    "find_elements" => this.ExecuteFindElements(arguments),
                    "get_bindings" => this.ExecuteGetBindings(),
                    _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
                };

                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(result, JsonOptions)
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error: {ex.Message}"
                        }
                    },
                    isError = true
                };
            }
        });
    }

    private object ExecuteGetVisualTree(JsonElement arguments)
    {
        var maxDepth = 10;
        var includeProperties = false;

        if (arguments.ValueKind != JsonValueKind.Undefined)
        {
            if (arguments.TryGetProperty("maxDepth", out var depthProp))
            {
                maxDepth = Math.Min(50, Math.Max(1, depthProp.GetInt32()));
            }

            if (arguments.TryGetProperty("includeProperties", out var propsProp))
            {
                includeProperties = propsProp.GetBoolean();
            }
        }

        var root = this.getRootTreeItem();
        if (root is null)
        {
            return new { error = "No visual tree available" };
        }

        return new
        {
            tree = this.BuildTreeNode(root, 0, maxDepth, includeProperties, "")
        };
    }

    private object BuildTreeNode(TreeItem item, int currentDepth, int maxDepth, bool includeProperties, string path)
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
            node["keyProperties"] = this.GetKeyProperties(depObj);
        }

        if (currentDepth < maxDepth && item.Children.Count > 0)
        {
            var children = new List<object>();
            for (var i = 0; i < item.Children.Count; i++)
            {
                var childPath = string.IsNullOrEmpty(path) ? i.ToString() : $"{path}/{i}";
                children.Add(this.BuildTreeNode(item.Children[i], currentDepth + 1, maxDepth, includeProperties, childPath));
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

    private Dictionary<string, object?> GetKeyProperties(DependencyObject depObj)
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

    private object ExecuteGetSelectedElement()
    {
        var selection = this.getCurrentSelection();
        if (selection is null)
        {
            return new { error = "No element selected" };
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
            result["properties"] = this.GetAllProperties(depObj, null);
        }

        return result;
    }

    private object ExecuteGetElementProperties(JsonElement arguments)
    {
        var path = arguments.GetProperty("path").GetString();
        var filter = arguments.TryGetProperty("filter", out var filterProp) ? filterProp.GetString() : null;

        var element = this.FindElementByPath(path);
        if (element is null)
        {
            return new { error = $"Element not found at path: {path}" };
        }

        if (element.Target is not DependencyObject depObj)
        {
            return new { error = "Element is not a DependencyObject" };
        }

        return new
        {
            type = element.TargetType.Name,
            name = element.Name,
            path,
            properties = this.GetAllProperties(depObj, filter)
        };
    }

    private List<Dictionary<string, object?>> GetAllProperties(DependencyObject depObj, string? filter)
    {
        var properties = new List<Dictionary<string, object?>>();

        // Get dependency properties
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
                var valueStr = this.FormatValue(value);

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

    private string FormatValue(object? value)
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

        // For complex types, just return the type name
        return $"[{type.Name}]";
    }

    private object ExecuteSelectElement(JsonElement arguments)
    {
        var path = arguments.GetProperty("path").GetString();
        var element = this.FindElementByPath(path);

        if (element is null)
        {
            return new { error = $"Element not found at path: {path}" };
        }

        this.selectItem(element.Target);

        return new
        {
            success = true,
            selected = new
            {
                type = element.TargetType.Name,
                name = element.Name,
                path
            }
        };
    }

    private object ExecuteFindElements(JsonElement arguments)
    {
        var typeName = arguments.TryGetProperty("typeName", out var typeProp) ? typeProp.GetString() : null;
        var elementName = arguments.TryGetProperty("elementName", out var nameProp) ? nameProp.GetString() : null;
        var maxResults = arguments.TryGetProperty("maxResults", out var maxProp) ? maxProp.GetInt32() : 20;

        if (string.IsNullOrEmpty(typeName) && string.IsNullOrEmpty(elementName))
        {
            return new { error = "Either typeName or elementName must be provided" };
        }

        var root = this.getRootTreeItem();
        if (root is null)
        {
            return new { error = "No visual tree available" };
        }

        var results = new List<object>();
        this.SearchTree(root, typeName, elementName, "", results, maxResults);

        return new
        {
            count = results.Count,
            elements = results
        };
    }

    private void SearchTree(TreeItem item, string? typeName, string? elementName, string path, List<object> results, int maxResults)
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
            this.SearchTree(item.Children[i], typeName, elementName, childPath, results, maxResults);
        }
    }

    private object ExecuteGetBindings()
    {
        var selection = this.getCurrentSelection();
        if (selection is null)
        {
            return new { error = "No element selected" };
        }

        if (selection.Target is not DependencyObject depObj)
        {
            return new { error = "Selected element is not a DependencyObject" };
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

        return new
        {
            elementType = selection.TargetType.Name,
            elementName = selection.Name,
            bindingCount = bindings.Count,
            bindings
        };
    }

    private TreeItem? FindElementByPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return this.getRootTreeItem();
        }

        var root = this.getRootTreeItem();
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

    private async Task SendJsonResponseAsync(HttpListenerResponse response, object data, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(data, JsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);

        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
#endif

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
