using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snoop
{
    /// <summary>
    /// Interaction logic for EmbeddedShellView.xaml
    /// </summary>
    public partial class EmbeddedShellView : UserControl
    {
        private readonly Runspace runspace;
        private readonly PSInvocationSettings invocationSettings;
        private int historyIndex;

        public EmbeddedShellView()
        {
            InitializeComponent();

            this.Unloaded += delegate { runspace.Dispose(); };

            commandTextBox.PreviewKeyDown += OnCommandTextBoxPreviewKeyDown;

            // ignore execution-policy
            var iis = InitialSessionState.CreateDefault();
            iis.AuthorizationManager = new AuthorizationManager(Guid.NewGuid().ToString());

            this.invocationSettings = new PSInvocationSettings();
            this.invocationSettings.AddToHistory = true;
            this.invocationSettings.ErrorActionPreference = ActionPreference.Stop;

            this.runspace = RunspaceFactory.CreateRunspace(iis);
            this.runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            this.runspace.ApartmentState = ApartmentState.STA;
            this.runspace.Open();
        }

        private void OnCommandTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    ++this.historyIndex;
                    SetCommandTextToHistory(this.historyIndex);
                    break;
                case Key.Down:
                    --this.historyIndex;
                    SetCommandTextToHistory(this.historyIndex);
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

                    if (pipe.HadErrors)
                    {
                        foreach (var item in pipe.Error.ReadToEnd())
                        {
                            outputTextBox.AppendText(item.ToString());
                        }
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
            if (history <= 0)
            {
                return null;
            }

            using (var pipe = this.runspace.CreatePipeline("get-history -count " + history, false))
            {
                var results = pipe.Invoke();
                if (results.Count > 0)
                {
                    dynamic item = results[0];
                    return (string)item.CommandLine;
                }

                return null;
            }
        }
    }
}