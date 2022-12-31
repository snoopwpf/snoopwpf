namespace Snoop.Infrastructure.Diagnostics.Providers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using Snoop.Data.Tree;

public class UnresolvedDynamicResourceDiagnosticProvider : DiagnosticProvider
{
    private static readonly Type? resourceReferenceExpressionType = typeof(ResourceReferenceExpressionConverter).Assembly.GetType("System.Windows.ResourceReferenceExpression");

    private static readonly FieldInfo? resourceKeyFieldInfo = resourceReferenceExpressionType?.GetField("_resourceKey", BindingFlags.NonPublic | BindingFlags.Instance);

    public override string Name => "Unresolved {DynamicResource} usages";

    public override string Description => "Searches for {DynamicResource} usages that could not be resolved.";

    protected override IEnumerable<DiagnosticItem> GetDiagnosticItemsInternal(TreeItem treeItem)
    {
        if (treeItem.Target is not FrameworkElement frameworkElement)
        {
            yield break;
        }

        if (resourceReferenceExpressionType is null
            || resourceKeyFieldInfo is null)
        {
            yield break;
        }

        foreach (PropertyDescriptor? property in TypeDescriptor.GetProperties(frameworkElement.GetType()))
        {
            if (property is null)
            {
                continue;
            }

            var dpd = DependencyPropertyDescriptor.FromProperty(property);
            if (dpd is null)
            {
                continue;
            }

            var localValue = frameworkElement.ReadLocalValue(dpd.DependencyProperty);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (localValue is null)
            {
                continue;
            }

            // ResourceReferenceExpression is internal...
            var localValueType = localValue.GetType();

            if (resourceReferenceExpressionType.IsAssignableFrom(localValueType))
            {
                var resourceKey = resourceKeyFieldInfo.GetValue(localValue);

                if (resourceKey is null)
                {
                    continue;
                }

                var resource = frameworkElement.TryFindResource(resourceKey);
                if (resource is null)
                {
                    yield return
                        new(this,
                            "Resource not resolved",
                            $"The resource '{resourceKey}' on property '{dpd.DisplayName}' could not be resolved.",
                            DiagnosticArea.Resource,
                            DiagnosticLevel.Error)
                        {
                            TreeItem = treeItem,
                            Dispatcher = frameworkElement.Dispatcher,
                            SourceObject = frameworkElement
                        };
                }
            }
        }
    }
}