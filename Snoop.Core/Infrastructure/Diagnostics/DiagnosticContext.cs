namespace Snoop.Infrastructure.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Snoop.Data;
using Snoop.Data.Tree;
using Snoop.Infrastructure.Diagnostics.Providers;

public sealed class DiagnosticContext : IDisposable, INotifyPropertyChanged
{
    public DiagnosticContext(TreeService treeService)
        : this()
    {
        this.TreeService = treeService;
    }

    public DiagnosticContext()
    {
        this.DiagnosticProviders = new();
        this.DiagnosticItems = new SuspendableObservableCollection<DiagnosticItem>();

        this.DiagnosticProviders.Add(new FreezeFreezablesDiagnosticProvider());
        this.DiagnosticProviders.Add(new LocalResourceDefinitionsDiagnosticProvider());
        this.DiagnosticProviders.Add(new NonVirtualizedListsDiagnosticProvider());
        this.DiagnosticProviders.Add(new UnresolvedDynamicResourceDiagnosticProvider());
        this.DiagnosticProviders.Add(new BindingLeakDiagnosticProvider());
#if USE_WPF_BINDING_DIAG
        // todo: add providers
        //this.DiagnosticProviders.Add(new BindingDiagnosticProvider());
        //System.Windows.Diagnostics.ResourceDictionaryDiagnostics.StaticResourceResolved += this.ResourceDictionaryDiagnosticsOnStaticResourceResolved;
#endif

        foreach (var diagnosticProvider in this.DiagnosticProviders)
        {
            diagnosticProvider.IsActive = TransientSettingsData.Current?.EnableDiagnostics ?? true;
            diagnosticProvider.PropertyChanged += this.HandleDiagnosticProviderPropertyChanged;
        }
    }

#if USE_WPF_BINDING_DIAG
    private void ResourceDictionaryDiagnosticsOnStaticResourceResolved(object? sender, System.Windows.Diagnostics.StaticResourceResolvedEventArgs e)
    {
    }
#endif

    public TreeService? TreeService { get; }

    public ObservableCollection<DiagnosticProvider> DiagnosticProviders { get; } = new();

    public SuspendableObservableCollection<DiagnosticItem> DiagnosticItems { get; } = new();

    public void Add(DiagnosticItem item)
    {
        this.DiagnosticItems.Add(item);
    }

    public void AddRange(IEnumerable<DiagnosticItem> items)
    {
        foreach (var item in items)
        {
            this.DiagnosticItems.Add(item);
        }
    }

    public void TreeItemDisposed(TreeItem treeItem)
    {
        foreach (var item in this.DiagnosticItems.ToList())
        {
            if (item.TreeItem == treeItem)
            {
                this.DiagnosticItems.Remove(item);
            }
        }
    }

    public void Analyze(TreeItem item)
    {
        foreach (var diagnosticProvider in this.DiagnosticProviders)
        {
            this.Analyze(item, diagnosticProvider);
        }
    }

    public void Analyze(TreeItem item, DiagnosticProvider diagnosticProvider)
    {
        this.AddRange(diagnosticProvider.GetDiagnosticItems(item));
    }

    public void AnalyzeTree()
    {
        if (this.TreeService?.RootTreeItem is null)
        {
            return;
        }

        using var suspender = this.DiagnosticItems.SuspendNotifications();

        // Only add global Diagnostics if with we should analyze the whole tree
        foreach (var diagnosticProvider in this.DiagnosticProviders)
        {
            this.AddRange(diagnosticProvider.GetGlobalDiagnosticItems());
        }

        this.AnalyzeTree(this.TreeService.RootTreeItem);
    }

    public void AnalyzeTree(DiagnosticProvider diagnosticProvider)
    {
        if (this.TreeService?.RootTreeItem is null)
        {
            return;
        }

        using var suspender = this.DiagnosticItems.SuspendNotifications();

        // Only add global Diagnostics if with we should analyze the whole tree
        this.AddRange(diagnosticProvider.GetGlobalDiagnosticItems());

        this.AnalyzeTree(this.TreeService.RootTreeItem, diagnosticProvider);
    }

    private void AnalyzeTree(TreeItem item)
    {
        this.Analyze(item);

        if (item.OmitChildren == false)
        {
            foreach (var child in item.Children)
            {
                this.AnalyzeTree(child);
            }
        }
    }

    private void AnalyzeTree(TreeItem item, DiagnosticProvider diagnosticProvider)
    {
        this.Analyze(item, diagnosticProvider);

        if (item.OmitChildren == false)
        {
            foreach (var child in item.Children)
            {
                this.AnalyzeTree(child, diagnosticProvider);
            }
        }
    }

    private void HandleDiagnosticProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.Equals(nameof(DiagnosticProvider.IsActive), StringComparison.Ordinal) == true
            && sender is DiagnosticProvider diagnosticProvider)
        {
            // Always remove the diagnostics from the affected provider first
            foreach (var diagnosticItem in this.DiagnosticItems.Where(x => ReferenceEquals(x.DiagnosticProvider, diagnosticProvider)).ToList())
            {
                this.DiagnosticItems.Remove(diagnosticItem);
            }

            if (diagnosticProvider.IsActive)
            {
                this.AnalyzeTree(diagnosticProvider);
            }
        }
    }

    public void Dispose()
    {
        foreach (var provider in this.DiagnosticProviders)
        {
            provider.Dispose();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}