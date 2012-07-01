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
                    break;
                case Key.Down:
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
    }
}