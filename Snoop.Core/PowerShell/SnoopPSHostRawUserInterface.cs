// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.PowerShell;

using System;
using System.Management.Automation.Host;
using System.Windows.Controls;

internal class SnoopPSHostRawUserInterface : PSHostRawUserInterface
{
    private readonly TextBox outputTextBox;

    public SnoopPSHostRawUserInterface(TextBox outputTextBox)
    {
        this.outputTextBox = outputTextBox;
    }

    public override KeyInfo ReadKey(ReadKeyOptions options)
    {
        throw new NotImplementedException();
    }

    public override void FlushInputBuffer()
    {
        throw new NotImplementedException();
    }

    public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
    {
        throw new NotImplementedException();
    }

    public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
    {
        // Handle clear/cls etc.
        if (fill.BufferCellType == BufferCellType.Complete
            && rectangle == new Rectangle(-1, -1, -1, -1))
        {
            this.outputTextBox.Clear();
        }
    }

    public override BufferCell[,] GetBufferContents(Rectangle rectangle)
    {
        throw new NotImplementedException();
    }

    public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
    {
        throw new NotImplementedException();
    }

    public override ConsoleColor ForegroundColor { get; set; }

    public override ConsoleColor BackgroundColor { get; set; }

    public override Coordinates CursorPosition { get; set; }

    public override Coordinates WindowPosition { get; set; }

    public override int CursorSize { get; set; }

    public override Size BufferSize { get; set; }

    public override Size WindowSize { get; set; }

    public override Size MaxWindowSize
    {
        get { throw new NotImplementedException(); }
    }

    public override Size MaxPhysicalWindowSize
    {
        get { throw new NotImplementedException(); }
    }

    public override bool KeyAvailable
    {
        get { throw new NotImplementedException(); }
    }

    public override string? WindowTitle { get; set; }
}