# MCP (Model Context Protocol) Integration Guide

## Overview

Snoop now includes an integrated MCP server that enables AI assistants like Claude to interact with and inspect WPF applications in real-time. This powerful feature bridges traditional debugging tools with modern AI assistance, enabling automated UI inspection, debugging, and analysis.

## What is MCP?

The Model Context Protocol (MCP) is an open protocol that standardizes how AI applications interact with external tools and data sources. By integrating MCP into Snoop, AI assistants can now:

- Browse and analyze WPF visual trees
- Inspect element properties and bindings
- Search for specific UI elements
- Capture visual screenshots
- Help diagnose layout and binding issues
- Generate detailed bug reports

## Getting Started

### Prerequisites

- Snoop 6.1.0 or later
- .NET 6.0+ (for fallback implementation) or .NET 8.0+ (for official MCP SDK)
- An MCP-compatible AI assistant (e.g., Claude Desktop)

### Starting the MCP Server

1. Launch Snoop and attach to a WPF application
2. Click the **MCP Server** button in the menu bar
3. In the MCP Server window, click **Start Server**
4. The server will start on an available port between 47700-47799
5. Copy the SSE endpoint URL shown in the window

### Configuring Claude Desktop

To connect Claude Desktop to the Snoop MCP server:

1. Open Claude Desktop settings
2. Navigate to the MCP servers configuration
3. Add a new server configuration:

```json
{
  "mcpServers": {
    "snoop": {
      "command": "sse",
      "args": ["http://localhost:47700/sse"]
    }
  }
}
```

Note: Replace `47700` with the actual port shown in your Snoop MCP Server window.

4. Restart Claude Desktop
5. Claude will now have access to the Snoop MCP tools

## Available MCP Tools

The Snoop MCP server exposes 7 specialized tools:

### 1. get_visual_tree

Retrieves the hierarchical structure of UI elements in the WPF application.

**Parameters:**
- `maxDepth` (number, 1-50): Maximum tree depth to retrieve (default: 10)
- `includeProperties` (boolean): Include key properties for each element (default: false)

**Returns:**
- JSON tree structure with type, name, path, and optional properties

**Example Use Case:**
"Show me the visual tree structure of the current window"

### 2. get_selected_element

Gets detailed information about the currently selected element in Snoop.

**Parameters:** None

**Returns:**
- Element type
- Key properties
- Binding errors
- Child count

**Example Use Case:**
"What properties does the selected button have?"

### 3. get_element_properties

Retrieves all properties of a specific element by path.

**Parameters:**
- `path` (string, required): Element path (e.g., "0/2/1")
- `filter` (string, optional): Filter properties by name

**Returns:**
- Filtered list of properties with names, values, and types

**Example Use Case:**
"Show me all margin-related properties for the element at path 0/1/3"

### 4. select_element

Programmatically selects an element in the Snoop UI.

**Parameters:**
- `path` (string, required): Element path to select

**Returns:**
- Success confirmation

**Side Effect:**
- Updates the Snoop UI to show the selected element

**Example Use Case:**
"Select the third child element at the root level"

### 5. find_elements

Searches the visual tree for elements matching specific criteria.

**Parameters:**
- `typeName` (string, optional): Element type to search for (e.g., "Button", "TextBox")
- `elementName` (string, optional): Element name to search for
- `maxResults` (number, optional): Maximum number of results to return (default: 20)

**Returns:**
- List of matching elements with paths and details

**Example Use Case:**
"Find all TextBox elements in the application"

### 6. get_bindings

Analyzes data bindings on the currently selected element.

**Parameters:** None

**Returns:**
- Binding paths
- Binding modes
- Data sources
- Binding errors and warnings

**Example Use Case:**
"Are there any binding errors on the selected element?"

### 7. get_element_preview

Captures a visual screenshot of a specific element.

**Parameters:**
- `maxWidth` (number, optional): Maximum width in pixels (default: 800)
- `maxHeight` (number, optional): Maximum height in pixels (default: 600)

**Returns:**
- Base64-encoded PNG image

**Example Use Case:**
"Show me what the selected element looks like"

## Use Cases and Workflows

### Debugging Layout Issues

**Scenario:** A panel is not displaying correctly.

```
User: "The main panel looks wrong. Can you help me debug it?"

AI with Snoop MCP:
1. Uses get_visual_tree to understand the layout hierarchy
2. Uses find_elements to locate the panel
3. Uses select_element to focus on it
4. Uses get_element_properties to check Width, Height, Margin, Padding
5. Uses get_element_preview to capture a visual screenshot
6. Identifies the issue (e.g., wrong alignment or margin)
```

### Finding Binding Errors

**Scenario:** Data is not displaying in a DataGrid.

```
User: "My DataGrid is empty even though I have data."

AI with Snoop MCP:
1. Uses find_elements to locate the DataGrid
2. Uses get_bindings to check binding configuration
3. Identifies binding errors or incorrect paths
4. Suggests fixes based on the binding analysis
```

### Performance Analysis

**Scenario:** Application feels slow during scrolling.

```
User: "Scrolling is laggy in my list."

AI with Snoop MCP:
1. Uses get_visual_tree with includeProperties to analyze the list structure
2. Identifies virtualization issues or excessive elements
3. Uses get_element_properties to check VirtualizingPanel properties
4. Suggests optimizations (e.g., enable virtualization, reduce complexity)
```

### Generating Bug Reports

**Scenario:** Need to report a visual bug to the team.

```
User: "Generate a bug report for this layout issue."

AI with Snoop MCP:
1. Uses get_visual_tree to document the affected hierarchy
2. Uses get_element_properties to capture relevant property values
3. Uses get_element_preview to create visual evidence
4. Uses get_bindings to check for related binding issues
5. Generates a comprehensive markdown bug report with screenshots
```

## Technical Architecture

### Server Implementation

The MCP server is implemented with two variants:

**Official SDK (NET 8.0+):**
- Uses `ModelContextProtocol.AspNetCore` package
- Built on ASP.NET Core WebApplication
- Leverages official MCP SDK for protocol handling

**Fallback (.NET 6.0, .NET Framework 4.6.2+):**
- Custom HttpListener-based implementation
- Manual JSON-RPC 2.0 protocol handling
- Server-Sent Events via streaming response

### Communication Flow

```
AI Assistant (Claude)
    ↕ (SSE connection)
MCP Server (localhost:47700-47799)
    ↕ (Dispatcher invocation)
Snoop UI (WPF Application)
    ↕ (Tree inspection)
Target WPF Application
```

All operations are executed on the WPF UI thread via Dispatcher to ensure thread safety.

### Security Considerations

- Server only listens on localhost (not accessible remotely)
- No authentication required (local-only access)
- Server can be stopped at any time from the MCP Server window
- All operations are read-only except select_element (which only changes Snoop's UI)

## Troubleshooting

### Server Won't Start

**Problem:** Server fails to start or shows an error.

**Solutions:**
- Check if ports 47700-47799 are already in use
- Ensure firewall is not blocking localhost connections
- Verify .NET runtime version meets requirements
- Check Snoop logs for detailed error messages

### AI Assistant Can't Connect

**Problem:** Claude Desktop shows "Connection failed" for Snoop server.

**Solutions:**
- Verify the server is running (check MCP Server window)
- Confirm the correct port in Claude Desktop configuration
- Restart Claude Desktop after configuration changes
- Check the SSE endpoint URL matches exactly

### Tools Not Responding

**Problem:** MCP tools execute but return no data or errors.

**Solutions:**
- Ensure a WPF application is attached to Snoop
- Verify the visual tree is loaded in Snoop
- Check if the requested element paths are valid
- Try refreshing the tree in Snoop

### Performance Issues

**Problem:** Tools are slow or Snoop becomes unresponsive.

**Solutions:**
- Reduce maxDepth when calling get_visual_tree
- Limit maxResults when using find_elements
- Disable includeProperties if not needed
- Close the MCP Server window when not in use

## Best Practices

1. **Start specific**: Use find_elements to locate specific elements before inspecting properties
2. **Limit depth**: When exploring trees, start with lower maxDepth and increase as needed
3. **Use filters**: Apply filters in get_element_properties to reduce noise
4. **Combine tools**: Use multiple tools in sequence for comprehensive analysis
5. **Capture evidence**: Use get_element_preview to document visual states
6. **Check bindings early**: Always check get_bindings when debugging data display issues

## Limitations

- Server is localhost-only (cannot be accessed remotely)
- Requires an active Snoop session attached to a WPF application
- Element paths are relative to current tree state (can change if tree updates)
- Screenshot quality depends on element size and complexity
- Some properties may not be readable due to security or access restrictions

## Future Enhancements

Potential features under consideration:

- Multi-application support (manage multiple attached apps)
- Property editing via MCP (write operations)
- Event monitoring and triggering
- Performance profiling integration
- Custom tool extensibility API
- WebSocket transport option
- Authentication for remote scenarios

## Contributing

If you have ideas for improving the MCP integration or encounter issues, please:

1. Check existing issues at [GitHub Issues](https://github.com/snoopwpf/snoopwpf/issues)
2. Submit new issues with detailed reproduction steps
3. Contribute pull requests with enhancements

## Resources

- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [Snoop GitHub Repository](https://github.com/snoopwpf/snoopwpf)
- [Claude Desktop](https://claude.ai/desktop)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

## License

The MCP integration follows the same license as Snoop (MIT License).
