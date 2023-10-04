namespace Snoop.Windows;

using System;
using System.Windows;
using System.Windows.Forms.Integration;
using Snoop.Data;
using Snoop.Infrastructure;

public abstract class SnoopMainBaseWindow : SnoopBaseWindow
{
    private Window? ownerWindow;

    public object? RootObject { get; private set; }

    public abstract object? Target { get; set; }

    public Window Inspect(object rootObject)
    {
        ExceptionHandler.AddExceptionHandler(this.Dispatcher);

        this.RootObject = rootObject;

        this.Load(rootObject);

        this.ownerWindow = SnoopWindowUtils.FindOwnerWindow(this);

        if (TransientSettingsData.Current?.SetOwnerWindow == true)
        {
            this.Owner = this.ownerWindow;
        }
        else if (this.ownerWindow is not null)
        {
            // if we have an owner window, but the owner should not be set, we still have to close ourself if the potential owner window got closed
            this.ownerWindow.Closed += this.OnOwnerWindowOnClosed;
        }

        LogHelper.WriteLine("Showing snoop UI...");

        if (System.Windows.Forms.Application.OpenForms.Count > 0)
        {
            // this is windows forms -> wpf interop

            // call ElementHost.EnableModelessKeyboardInterop to allow the Snoop UI window
            // to receive keyboard messages. if you don't call this method,
            // you will be unable to edit properties in the property grid for windows forms interop.
            ElementHost.EnableModelessKeyboardInterop(this);
        }

        this.ShowActivated = TransientSettingsData.Current?.ShowActivated is not false;
        this.Show();

        LogHelper.WriteLine("Shown snoop UI.");

        return this;
    }

    private void OnOwnerWindowOnClosed(object? o, EventArgs eventArgs)
    {
        if (this.ownerWindow is not null)
        {
            this.ownerWindow.Closed -= this.OnOwnerWindowOnClosed;
        }

        this.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        ExceptionHandler.RemoveExceptionHandler(this.Dispatcher);

        base.OnClosed(e);
    }

    protected abstract void Load(object rootToInspect);
}