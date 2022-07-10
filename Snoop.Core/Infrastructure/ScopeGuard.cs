namespace Snoop.Infrastructure;

using System;

public class ScopeGuard : IDisposable
{
    public ScopeGuard(Action? enterAction = null, Action? exitAction = null)
    {
        this.EnterAction = enterAction;
        this.ExitAction = exitAction;
    }

    public Action? EnterAction { get; }

    public Action? ExitAction { get; }

    public ScopeGuard Guard()
    {
        this.EnterAction?.Invoke();
        return this;
    }

    public void Dispose()
    {
        this.ExitAction?.Invoke();
    }
}