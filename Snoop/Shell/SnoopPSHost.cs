// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using System.Threading;

namespace Snoop.Shell
{
    internal class SnoopPSHost : PSHost
    {
        private readonly Guid id = Guid.NewGuid();
        private readonly SnoopPSHostUserInterface ui;
        private readonly PSObject privateData;
        private readonly Hashtable privateHashtable;

        public SnoopPSHost(Action<string> onOutput)
        {
            this.ui = new SnoopPSHostUserInterface();
            this.ui.OnDebug += onOutput;
            this.ui.OnError += onOutput;
            this.ui.OnVerbose += onOutput;
            this.ui.OnWarning += onOutput;
            this.ui.OnWrite += onOutput;

            this.privateHashtable = new Hashtable();
            this.privateData = new PSObject(this.privateHashtable);
        }

        public override void SetShouldExit(int exitCode)
        {
        }

        public override void EnterNestedPrompt()
        {
        }

        public override void ExitNestedPrompt()
        {
        }

        public override void NotifyBeginApplication()
        {
        }

        public override void NotifyEndApplication()
        {
        }

        public override CultureInfo CurrentCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
        }

        public override Guid InstanceId
        {
            get { return this.id; }
        }

        public override string Name
        {
            get { return this.id.ToString(); }
        }

        public override PSHostUserInterface UI
        {
            get { return this.ui; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override PSObject PrivateData
        {
            get { return this.privateData; }
        }

        public object this[string name]
        {
            get { return this.privateHashtable[name]; }
            set { this.privateHashtable[name] = value; }
        }
    }
}