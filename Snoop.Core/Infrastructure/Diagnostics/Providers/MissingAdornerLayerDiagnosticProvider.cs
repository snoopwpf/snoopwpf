namespace Snoop.Infrastructure.Diagnostics.Providers;

public class MissingAdornerLayerDiagnosticProvider : DiagnosticProvider
{
    public static readonly MissingAdornerLayerDiagnosticProvider Instance = new();

    public override string Name => "Missing adorner layer";

    public override string Description => "No adorner layer for the selected element could be found";
}