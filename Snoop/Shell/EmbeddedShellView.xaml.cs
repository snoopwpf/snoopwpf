// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snoop.Shell
{
    /// <summary>
    /// Interaction logic for EmbeddedShellView.xaml
    /// </summary>
    public partial class EmbeddedShellView : UserControl
    {
        private readonly Runspace runspace;
        private int historyIndex;

        public EmbeddedShellView()
        {
            InitializeComponent();

            this.Unloaded += delegate { runspace.Dispose(); };

            commandTextBox.PreviewKeyDown += OnCommandTextBoxPreviewKeyDown;
            ToolTipService.SetToolTip(commandTextBox, @"
F12 - Reload profile
");

            // ignore execution-policy
            var iis = InitialSessionState.CreateDefault();
            iis.AuthorizationManager = new AuthorizationManager(Guid.NewGuid().ToString());

            this.runspace = RunspaceFactory.CreateRunspace(new SnoopPSHost(x => this.outputTextBox.AppendText(x)), iis);
            this.runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            this.runspace.ApartmentState = ApartmentState.STA;
            this.runspace.Open();
        }

        public void Start()
        {
            string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Scripts");
            Invoke(string.Format("import-module \"{0}\"", Path.Combine(folder, "Snoop.psm1")));

            Invoke("write-host 'Welcome to the Snoop PowerShell console!'");
            Invoke("write-host '----------------------------------------'");
            Invoke("write-host 'To get started, try the $root and $selected variables.'");

            string name = "SnoopProfile.ps1";
            if (!LoadProfile(Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), name)))
            {
                LoadProfile(Path.Combine(folder, name));
            }
        }

        private bool LoadProfile(string path)
        {
            if (File.Exists(path))
            {
                Invoke(string.Format("$profile = '{0}'; . $profile", path));
                return true;
            }

            return false;
        }

        private void OnCommandTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    SetCommandTextToHistory(++this.historyIndex);
                    break;
                case Key.Down:
                    if (this.historyIndex - 1 <= 0)
                    {
                        this.commandTextBox.Clear();
                    }
                    else
                    {
                        SetCommandTextToHistory(--this.historyIndex);
                    }
                    break;
                case Key.Return:
                    outputTextBox.AppendText(Environment.NewLine);
                    outputTextBox.AppendText(commandTextBox.Text);
                    outputTextBox.AppendText(Environment.NewLine);

                    Invoke(commandTextBox.Text);
                    commandTextBox.Clear();
                    break;
            }
        }

        public void SetVariable(string name, object instance)
        {
            this.runspace.SessionStateProxy.SetVariable(name, instance);
        }

        private void Invoke(string script)
        {
            this.historyIndex = 0;

            try
            {
                using (var pipe = this.runspace.CreatePipeline(script, true))
                {
                    var cmd = new Command("Out-String");
                    cmd.Parameters.Add("Width", 500);
                    pipe.Commands.Add(cmd);

                    foreach (var item in pipe.Invoke())
                    {
                        outputTextBox.AppendText(item.ToString());
                    }

                    foreach (var item in pipe.Error.ReadToEnd())
                    {
                        outputTextBox.AppendText(item.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                outputTextBox.AppendText(ex.Message);
            }

            outputTextBox.ScrollToEnd();
        }

        private void SetCommandTextToHistory(int history)
        {
            var cmd = GetHistoryCommand(history);
            if (cmd != null)
            {
                commandTextBox.Text = cmd;
                commandTextBox.SelectionStart = cmd.Length;
            }
        }

        private string GetHistoryCommand(int history)
        {
            using (var pipe = this.runspace.CreatePipeline("get-history -count " + history))
            {
                var results = pipe.Invoke();
                if (results.Count > 0)
                {
                    var item = results[0];
                    return (string)item.Properties["CommandLine"].Value;
                }

                return null;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.F12)
            {
                Invoke(". $profile");
            }
        }
    }
}