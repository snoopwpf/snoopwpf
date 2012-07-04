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

            // ignore execution-policy
            var iis = InitialSessionState.CreateDefault();
            iis.AuthorizationManager = new AuthorizationManager(Guid.NewGuid().ToString());

            this.runspace = RunspaceFactory.CreateRunspace(new SnoopPSHost(x => this.outputTextBox.AppendText(x)), iis);
            this.runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            this.runspace.ApartmentState = ApartmentState.STA;
            this.runspace.Open();

            LoadModule();
        }

        private void LoadModule()
        {
            string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Scripts");
            InvokeDirect(string.Format("import-module \"{0}\"", Path.Combine(folder, "Snoop.psm1")));

            string profile = Path.Combine(folder, "SnoopProfile.ps1");
            InvokeDirect(string.Format("if (test-path \"{0}\") {{ . \"{0}\" }}", profile));
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

            outputTextBox.AppendText(Environment.NewLine);
            outputTextBox.AppendText(script);
            outputTextBox.AppendText(Environment.NewLine);

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

        private Collection<PSObject> InvokeDirect(string script, bool addToHistory = false)
        {
            using (var pipe = this.runspace.CreatePipeline(script, addToHistory))
            {
                return pipe.Invoke();
            }
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
    }
}