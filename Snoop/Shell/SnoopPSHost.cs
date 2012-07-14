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
            ui = new SnoopPSHostUserInterface();
            ui.OnDebug += onOutput;
            ui.OnError += onOutput;
            ui.OnVerbose += onOutput;
            ui.OnWarning += onOutput;
            ui.OnWrite += onOutput;

            privateHashtable = new Hashtable();
            privateData = new PSObject(privateHashtable);
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
            get { return id; }
        }

        public override string Name
        {
            get { return id.ToString(); }
        }

        public override PSHostUserInterface UI
        {
            get { return ui; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override PSObject PrivateData
        {
            get { return privateData; }
        }

        public void SetVariable(string name, object value)
        {
            privateHashtable[name] = value;
        }
    }
}