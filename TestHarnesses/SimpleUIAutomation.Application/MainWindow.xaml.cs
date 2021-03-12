namespace SimpleUIAutomation.Application
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Windows;

    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.testTextBox.AppendText(DateTime.Now.ToLongDateString() + Environment.NewLine);
        }

        private void StartAutomation_OnClick(object sender, RoutedEventArgs e)
        {
            var scriptLines = new[]
            {
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
                "Input Test-TextBox Foo",
                "Input Test-TextBox Bar",
                "Click Test-Button",
                "Click Test-Button",
                "Click Test-Button",
            };

            var tempFile = Path.GetTempFileName();
            File.WriteAllLines(tempFile, scriptLines, Encoding.UTF8);

            var automationAssemblyName = "SimpleUIAutomation.dll";
            var automationAssemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "../../../SimpleUIAutomation/Debug/netcoreapp3.1", automationAssemblyName);
            var arguments = $"--targetPID {Process.GetCurrentProcess().Id} --assembly \"{automationAssemblyPath}\" --className SimpleUIAutomation.AutomationDriver --methodName StartAutomation --settingsFile \"{tempFile}\"";

            var startInfo = new ProcessStartInfo(@"..\..\..\..\..\bin\debug\Snoop.InjectorLauncher.x64.exe", arguments)
            {
                // Hide the console window
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }
    }
}