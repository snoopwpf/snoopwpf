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
        private readonly PowerShell shell;
        private readonly PSInvocationSettings invocationSettings;

        public EmbeddedShellView()
        {
            InitializeComponent();

            this.Unloaded += delegate
            {
                runspace.Dispose();
                shell.Dispose();
            };

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

            this.shell = PowerShell.Create(iis);
            this.shell.Runspace = runspace;
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
            try
            {
                shell.Commands.AddScript(script);
                shell.Commands.AddCommand("Out-String");
                shell.Commands.AddParameter("Width", 500);

                foreach (var item in shell.Invoke(null, this.invocationSettings))
                {
                    outputTextBox.AppendText(item.ToString());
                }

                foreach (var item in shell.Streams.Error)
                {
                    outputTextBox.AppendText(item.ToString());
                }
            }
            catch (Exception ex)
            {
                outputTextBox.AppendText(ex.ToString());
            }
            finally
            {
                shell.Commands.Clear();
                shell.Streams.ClearStreams();
            }

            outputTextBox.ScrollToEnd();
        }
    }
}