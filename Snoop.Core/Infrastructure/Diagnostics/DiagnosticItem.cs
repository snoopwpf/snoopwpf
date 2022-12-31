namespace Snoop.Infrastructure.Diagnostics;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using JetBrains.Annotations;
using Snoop.Data.Tree;

public class DiagnosticItem : INotifyPropertyChanged
{
    public DiagnosticItem(DiagnosticProvider diagnosticProvider, string? name = null, string? description = null, DiagnosticArea area = DiagnosticArea.Misc, DiagnosticLevel level = DiagnosticLevel.Info)
    {
        this.DiagnosticProvider = diagnosticProvider;
        this.Name = name ?? diagnosticProvider.Name;
        this.Description = description ?? diagnosticProvider.Description;
        this.Area = area;
        this.Level = level;
    }

    public DiagnosticProvider DiagnosticProvider { get; }

    public DiagnosticArea Area { get; set; }

    public DiagnosticLevel Level { get; set; }

    public string Name { get; }

    public string Description { get; }

    public object? SourceObject { get; set; }

    public TreeItem? TreeItem { get; set; }

    public Dispatcher? Dispatcher { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}