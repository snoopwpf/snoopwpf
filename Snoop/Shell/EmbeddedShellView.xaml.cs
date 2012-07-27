// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
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
        public const string DriveName = "snoop";

        public event Action<VisualTreeItem> ProviderLocationChanged;

        private readonly Runspace runspace;
        private readonly SnoopPSHost host;
        private int historyIndex;

        public EmbeddedShellView()
        {
            InitializeComponent();

            this.commandTextBox.PreviewKeyDown += OnCommandTextBoxPreviewKeyDown;
            ToolTipService.SetToolTip(this.commandTextBox, @"
F5 - Reload profile
F12 - Clear output
");

            // ignore execution-policy
            var iis = InitialSessionState.CreateDefault();
            iis.AuthorizationManager = new AuthorizationManager(Guid.NewGuid().ToString());
            iis.Providers.Add(new SessionStateProviderEntry(DriveName, typeof(VisualTreeProvider), string.Empty));

            this.host = new SnoopPSHost(x => this.outputTextBox.AppendText(x));
            this.runspace = RunspaceFactory.CreateRunspace(this.host, iis);
            this.runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            this.runspace.ApartmentState = ApartmentState.STA;
            this.runspace.Open();

            // default required if you intend to inject scriptblocks into the host application
            Runspace.DefaultRunspace = this.runspace;
        }

        public void Start()
        {
            Invoke(string.Format("new-psdrive {0} {0} -root /", DriveName));

            this.SetVariable(VisualTreeProvider.LocationChangedKeyAction, this.ProviderLocationChanged);
            
            string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Scripts");
            Invoke(string.Format("import-module \"{0}\"", Path.Combine(folder, "Snoop.psm1")));

            this.outputTextBox.Clear();
            Invoke("write-host 'Welcome to the Snoop PowerShell console!'");
            Invoke("write-host '----------------------------------------'");
            Invoke("write-host 'To get started, try using the $root and $selected variables.'");

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
                Invoke("write-host 'Profile loaded: $profile'");
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
                    this.outputTextBox.AppendText(Environment.NewLine);
                    this.outputTextBox.AppendText(this.commandTextBox.Text);
                    this.outputTextBox.AppendText(Environment.NewLine);

                    Invoke(this.commandTextBox.Text, true);
                    this.commandTextBox.Clear();
                    break;
            }
        }

        public void SetVariable(string name, object instance)
        {
            this.host[name] = instance;
            Invoke(string.Format("${0} = $host.PrivateData['{0}']", name));
        }

        public void Invoke(string script, bool addToHistory = false)
        {
            this.historyIndex = 0;

            try
            {
                using (var pipe = this.runspace.CreatePipeline(script, addToHistory))
                {
                    var cmd = new Command("Out-String");
                    cmd.Parameters.Add("Width", Math.Max(2, (int)(this.ActualWidth * 0.7)));
                    pipe.Commands.Add(cmd);

                    foreach (var item in pipe.Invoke())
                    {
                        this.outputTextBox.AppendText(item.ToString());
                    }

                    foreach (PSObject item in pipe.Error.ReadToEnd())
                    {
                        var error = (ErrorRecord)item.BaseObject;
                        this.OutputErrorRecord(error);
                    }
                }
            }
            catch (RuntimeException ex)
            {
                this.OutputErrorRecord(ex.ErrorRecord);
            }
            catch (Exception ex)
            {
                this.outputTextBox.AppendText(string.Format("Oops!  Uncaught exception invoking on the PowerShell runspace: {0}", ex.Message));
            }

            this.outputTextBox.ScrollToEnd();
        }

        private void OutputErrorRecord(ErrorRecord error)
        {
            this.outputTextBox.AppendText(string.Format("{0}{1}", error, error.InvocationInfo != null ? error.InvocationInfo.PositionMessage : string.Empty));
            this.outputTextBox.AppendText(string.Format("{1}  + CategoryInfo          : {0}", error.CategoryInfo, Environment.NewLine));
            this.outputTextBox.AppendText(string.Format("{1}  + FullyQualifiedErrorId : {0}", error.FullyQualifiedErrorId, Environment.NewLine));
        }

        private void SetCommandTextToHistory(int history)
        {
            var cmd = GetHistoryCommand(history);
            if (cmd != null)
            {
                this.commandTextBox.Text = cmd;
                this.commandTextBox.SelectionStart = cmd.Length;
            }
        }

        private string GetHistoryCommand(int history)
        {
            using (var pipe = this.runspace.CreatePipeline("get-history -count " + history, false))
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

            switch (e.Key)
            {
                case Key.F5:
                    Invoke("if ($profile) { . $profile }");
                    break;
                case Key.F12:
                    this.outputTextBox.Clear();
                    break;
            }
        }
    }
}