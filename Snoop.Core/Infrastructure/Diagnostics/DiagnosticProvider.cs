namespace Snoop.Infrastructure.Diagnostics;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Snoop.Data.Tree;

public abstract class DiagnosticProvider : IDisposable, INotifyPropertyChanged
{
    private bool isActive = true;

    public abstract string Name { get; }

    public abstract string Description { get; }

    public bool IsActive
    {
        get => this.isActive;
        set
        {
            if (value == this.isActive)
            {
                return;
            }

            this.isActive = value;
            this.OnPropertyChanged();
        }
    }

    public IEnumerable<DiagnosticItem> GetDiagnosticItems(TreeItem treeItem)
    {
        return this.IsActive == false || treeItem.ShouldBeAnalyzed == false
            ? Enumerable.Empty<DiagnosticItem>()
            : this.GetDiagnosticItemsInternal(treeItem);
    }

    protected virtual IEnumerable<DiagnosticItem> GetDiagnosticItemsInternal(TreeItem treeItem)
    {
        return Enumerable.Empty<DiagnosticItem>();
    }

    public IEnumerable<DiagnosticItem> GetGlobalDiagnosticItems()
    {
        return this.IsActive == false
            ? Enumerable.Empty<DiagnosticItem>()
            : this.GetGlobalDiagnosticItemsInternal();
    }

    protected virtual IEnumerable<DiagnosticItem> GetGlobalDiagnosticItemsInternal()
    {
        return Enumerable.Empty<DiagnosticItem>();
    }

    public virtual void Dispose()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}