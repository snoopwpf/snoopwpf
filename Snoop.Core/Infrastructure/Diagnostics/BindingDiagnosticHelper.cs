namespace Snoop.Infrastructure.Diagnostics;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Snoop.Infrastructure.Helpers;

public partial class BindingDiagnosticHelper : ICacheManaged
{
    public static readonly BindingDiagnosticHelper Instance = new();

    private BindingDiagnosticHelper()
    {
#if USE_WPF_BINDING_DIAG
            this.ActivateWPFBindingDiagnostic();
#endif
    }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        if (this.IsActive)
        {
#if USE_WPF_BINDING_DIAG
                this.ListenForWPFBindingDiagnostic();
#endif
        }
    }

    public void Dispose()
    {
        if (this.IsActive)
        {
#if USE_WPF_BINDING_DIAG
                this.UnListenForWPFBindingDiagnostic();
#endif
        }
    }

    public void TrySetBindingError(BindingExpressionBase bindingExpressionBase, DependencyObject dependencyObject, DependencyProperty dependencyProperty, Action<string> errorSetter)
    {
#if USE_WPF_BINDING_DIAG
            if (this.TryGetEntry(bindingExpressionBase, out var failedBinding))
            {
                errorSetter(failedBinding.Messages);
                return;
            }
#endif

        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var tracer = new TextWriterTraceListener(writer);
        var levelBefore = PresentationTraceSources.DataBindingSource.Switch.Level;

        var scopeGuard = new ScopeGuard(
            () =>
            {
                PresentationTraceSourcesHelper.EnsureRequiredLevel();
                PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);
                //PresentationTraceSources.DataBindingSource.Listeners.Add(new SnoopDebugListener()); // for debugging purposes
            },
            () =>
            {
                PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                writer.Dispose();

                if (PresentationTraceSources.DataBindingSource.Switch.Level != levelBefore)
                {
                    PresentationTraceSources.DataBindingSource.Switch.Level = levelBefore;
                }
            });

        // reset binding to get the error message.
        dependencyObject.ClearValue(dependencyProperty);

        scopeGuard.Guard(); // Start listening
        var binding = bindingExpressionBase.ParentBindingBase;
        BindingOperations.SetBinding(dependencyObject, dependencyProperty, binding);

        // this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
        dependencyObject.RunInDispatcherAsync(
            () =>
            {
                scopeGuard.Dispose();
                errorSetter(builder.ToString());
            }, DispatcherPriority.ApplicationIdle);
    }
}