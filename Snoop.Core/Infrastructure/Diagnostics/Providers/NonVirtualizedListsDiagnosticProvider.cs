namespace Snoop.Infrastructure.Diagnostics.Providers;

using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using Snoop.Data.Tree;

public class NonVirtualizedListsDiagnosticProvider : DiagnosticProvider
{
    private static readonly PropertyInfo? itemsHostPropertyInfo = typeof(ItemsControl).GetProperty("ItemsHost", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    private const int Threshold = 50;

    public override string Name => "Non virtualized long lists";

    public override string Description => $"Warns if an ItemsControl with more than {Threshold} items is not virtualized.";

    protected override IEnumerable<DiagnosticItem> GetDiagnosticItemsInternal(TreeItem treeItem)
    {
        if (treeItem.Target is not ItemsControl itemsControl
            || itemsHostPropertyInfo is null)
        {
            yield break;
        }

        if (itemsControl.Items.Count <= Threshold)
        {
            yield break;
        }

        var panel = itemsHostPropertyInfo.GetValue(itemsControl, null);

        if (panel is not null
            && panel is not VirtualizingPanel)
        {
            yield return
                new(this,
                    "Virtualize ItemsControl",
                    $"ItemsControl with {itemsControl.Items.Count} items should be virtualized.",
                    DiagnosticArea.Performance,
                    DiagnosticLevel.Warning)
                {
                    TreeItem = treeItem,
                    Dispatcher = itemsControl.Dispatcher,
                    SourceObject = itemsControl
                };
        }
    }
}