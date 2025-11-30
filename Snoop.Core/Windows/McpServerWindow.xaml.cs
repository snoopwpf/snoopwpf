// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Windows;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Snoop.Infrastructure.Helpers;
using Snoop.Infrastructure.MCP;

public partial class McpServerWindow : SnoopBaseWindow
{
    private readonly McpServer mcpServer;

    public McpServerWindow(McpServer mcpServer)
    {
        this.mcpServer = mcpServer;

        this.InitializeComponent();

        this.mcpServer.PropertyChanged += this.OnMcpServerPropertyChanged;
        this.UpdateUI();
    }

    private void OnMcpServerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.Dispatcher.Invoke(this.UpdateUI);
    }

    private void UpdateUI()
    {
        if (this.mcpServer.IsRunning)
        {
            this.StatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
            this.StatusText.Text = "Running";
            this.StartStopButton.Content = "Stop Server";

            this.SseEndpointTextBox.Text = this.mcpServer.SseEndpoint;
            this.BaseUrlTextBox.Text = this.mcpServer.ConnectionUrl;
            this.PortTextBox.Text = this.mcpServer.Port.ToString();

            // Get process name for config example
            using var process = Process.GetCurrentProcess();
            var processName = process.ProcessName;

            this.ConfigExample.Text = $@"Add this to your claude_desktop_config.json:

{{
  ""mcpServers"": {{
    ""snoop-{processName}"": {{
      ""command"": ""npx"",
      ""args"": [
        ""mcp-remote"",
        ""{this.mcpServer.SseEndpoint}""
      ]
    }}
  }}
}}

Or use any MCP client that supports SSE transport with:
  URL: {this.mcpServer.SseEndpoint}";
        }
        else
        {
            this.StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
            this.StatusText.Text = "Stopped";
            this.StartStopButton.Content = "Start Server";

            this.SseEndpointTextBox.Text = "(Server not running)";
            this.BaseUrlTextBox.Text = "(Server not running)";
            this.PortTextBox.Text = "-";

            this.ConfigExample.Text = "Start the server to see connection instructions.";
        }
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.mcpServer.IsRunning)
        {
            this.mcpServer.Stop();
        }
        else
        {
            var started = await this.mcpServer.StartAsync();
            if (!started)
            {
                MessageBox.Show(
                    "Failed to start MCP server. Could not find an available port.",
                    "MCP Server Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void CopySseButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.mcpServer.IsRunning)
        {
            ClipboardHelper.SetText(this.mcpServer.SseEndpoint);
        }
    }

    private void CopyBaseUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.mcpServer.IsRunning)
        {
            ClipboardHelper.SetText(this.mcpServer.ConnectionUrl);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        this.mcpServer.PropertyChanged -= this.OnMcpServerPropertyChanged;
        base.OnClosed(e);
    }
}
