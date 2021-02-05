namespace Snoop.Infrastructure.Diagnostics
{
    using System.Windows.Threading;
    using Snoop.Data.Tree;

    public class DiagnosticItem
    {
        public DiagnosticItem(DiagnosticProvider diagnosticProvider)
        {
            this.DiagnosticProvider = diagnosticProvider;
            this.Name = string.Empty;
            this.Description = string.Empty;
        }

        public DiagnosticItem(DiagnosticProvider diagnosticProvider, string name, string description)
        {
            this.DiagnosticProvider = diagnosticProvider;
            this.Name = name;
            this.Description = description;
        }

        public DiagnosticItem(DiagnosticProvider diagnosticProvider, string name, string description, DiagnosticArea area, DiagnosticLevel level)
            : this(diagnosticProvider, name, description)
        {
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
    }

    public enum DiagnosticLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum DiagnosticArea
    {
        Binding,
        Resource,
        Performance,
        Maintainability
    }
}
