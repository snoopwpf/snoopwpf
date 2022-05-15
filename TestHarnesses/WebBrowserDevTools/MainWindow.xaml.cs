namespace WebBrowserDevTools
{
    using System.Windows;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await this.webView.EnsureCoreWebView2Async(null);
            this.webView.CoreWebView2.OpenDevToolsWindow();
        }
    }
}
