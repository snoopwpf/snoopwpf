// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;

namespace Snoop.Shell
{
    internal class SnoopPSHostUserInterface : PSHostUserInterface
    {
        private readonly PSHostRawUserInterface rawUI = new SnoopPSHostRawUserInterface();
        
        public event Action<string> OnVerbose = delegate { };
        public event Action<string> OnDebug = delegate { };
        public event Action<string> OnWarning = delegate { };
        public event Action<string> OnError = delegate { };
        public event Action<string> OnWrite = delegate { };

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }

        public override void Write(string value)
        {
            OnWrite(value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            OnWrite(value);
        }

        public override void WriteLine(string value)
        {
            OnWrite(value + Environment.NewLine);
        }

        public override void WriteErrorLine(string value)
        {
            OnError(value + Environment.NewLine);
        }

        public override void WriteDebugLine(string message)
        {
            OnDebug(message + Environment.NewLine);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            OnWrite(record.ToString());
        }

        public override void WriteVerboseLine(string message)
        {
            OnVerbose(message + Environment.NewLine);
        }

        public override void WriteWarningLine(string message)
        {
            OnWarning(message + Environment.NewLine);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            throw new NotImplementedException();
        }

        public override PSHostRawUserInterface RawUI
        {
            get { return this.rawUI; }
        }
    }
}