// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.PowerShell;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Windows.Controls;

internal class SnoopPSHostUserInterface : PSHostUserInterface
{
    private readonly PSHostRawUserInterface rawUI;
    private readonly TextBox outputTextBox;

    public SnoopPSHostUserInterface(TextBox outputTextBox)
    {
        this.outputTextBox = outputTextBox;
        this.rawUI = new SnoopPSHostRawUserInterface(this.outputTextBox);
    }

    public event Action<string>? OnVerbose;

    public event Action<string>? OnDebug;

    public event Action<string>? OnWarning;

    public event Action<string>? OnError;

    public event Action<string>? OnWrite;

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
        this.OnWrite?.Invoke(value);
    }

    public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
    {
        this.OnWrite?.Invoke(value);
    }

    public override void WriteLine(string value)
    {
        this.OnWrite?.Invoke(value + Environment.NewLine);
    }

    public override void WriteErrorLine(string value)
    {
        this.OnError?.Invoke(value + Environment.NewLine);
    }

    public override void WriteDebugLine(string message)
    {
        this.OnDebug?.Invoke(message + Environment.NewLine);
    }

    public override void WriteProgress(long sourceId, ProgressRecord record)
    {
        this.OnWrite?.Invoke(record.ToString());
    }

    public override void WriteVerboseLine(string message)
    {
        this.OnVerbose?.Invoke(message + Environment.NewLine);
    }

    public override void WriteWarningLine(string message)
    {
        this.OnWarning?.Invoke(message + Environment.NewLine);
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