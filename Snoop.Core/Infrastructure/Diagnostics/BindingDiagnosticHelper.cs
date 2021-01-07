namespace Snoop.Infrastructure.Diagnostics
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
#if NET50
    using System.Windows.Diagnostics;
#endif
    using System.Windows.Threading;
    using JetBrains.Annotations;
    using Snoop.Infrastructure.Helpers;

    public class BindingDiagnosticHelper : IDisposable
    {
        public static readonly BindingDiagnosticHelper Instance = new BindingDiagnosticHelper();

#if NET50
        private readonly ObservableCollection<FailedBindingDetails> failedBindings = new();
#endif

        private BindingDiagnosticHelper()
        {
#if NET50
            this.FailedBindings = new(this.failedBindings);

            // check if VisualDiagnostics is enabled. If not we can't do much...
            if (ReflectionHelper.TryGetField(typeof(VisualDiagnostics), "s_IsEnabled", out var value)
                && value is bool boolValue
                && boolValue)
            {
                if (ReflectionHelper.TrySetProperty(typeof(BindingDiagnostics), "IsEnabled", true))
                {
                    this.IsActive = true; //WPFDiagnosticsHelper.TrySetField(typeof(DependencyObject).Assembly.GetType("AvTrace"), "_hasBeenRefreshed", true);
                }
            }

            if (this.IsActive)
            {
                // to get all failed binding results we have to increase the trace level
                // todo: when and how should we reset this value?
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.All;

                // this call causes BindingDiagnostics to start working as we set IsEnabled on it via reflection
                PresentationTraceSources.Refresh();

                // class TraceData
                //  => _avTrace (AVTrace)
                //      => _isEnabled

                // 1. AVTrace => _hasBeenRefreshed static field
                // 2. PresentationTraceSources.TraceRefresh
                // 3. BindingDiagnostics
                // 3. ??? VisualDiagnostics.IsEnabled

                // todo: when starting via snoop set env var ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO = true
                BindingDiagnostics.BindingFailed += this.BindingDiagnostics_BindingFailed;
            }
        #endif
        }

        public bool IsActive { get; }

#if NET50
        public ReadOnlyObservableCollection<FailedBindingDetails> FailedBindings { get; }
#endif

        public void Dispose()
        {
            if (this.IsActive)
            {
#if NET50
                BindingDiagnostics.BindingFailed -= this.BindingDiagnostics_BindingFailed;
#endif
            }
        }

#if NET50
        private void BindingDiagnostics_BindingFailed(object? sender, BindingFailedEventArgs e)
        {
            // We can only cache failures that have a binding
            if (e.Binding is null)
            {
                return;
            }

            if (this.TryGetEntry(e.Binding, out var failedBindingDetails))
            {
                this.failedBindings.Remove(failedBindingDetails);
            }

            {
                var binding = new WeakReference<BindingExpressionBase>(e.Binding);
                this.failedBindings.Add(new FailedBindingDetails(binding, e.EventType, e.Code, e.Message));
            }
        }
#endif

        public void TrySetBindingError(BindingExpressionBase binding, DependencyObject dependencyObject, DependencyProperty dependencyProperty, Action<string> errorSetter)
        {
#if NET50
            if (this.TryGetEntry(binding, out var failedBindingDetails))
            {
                errorSetter(failedBindingDetails.Message);
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
                    PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical | SourceLevels.Error;
                    PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);
                },
                () =>
                {
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                    writer.Dispose();
                    PresentationTraceSources.DataBindingSource.Switch.Level = levelBefore;
                })
                .Guard();

            // reset binding to get the error message.
            dependencyObject.ClearValue(dependencyProperty);
            BindingOperations.SetBinding(dependencyObject, dependencyProperty, binding.ParentBindingBase);

            // this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
            dependencyObject.RunInDispatcherAsync(
                () =>
                {
                    scopeGuard.Dispose();
                    errorSetter(builder.ToString());
                }, DispatcherPriority.ApplicationIdle);
        }

#if NET50
        public bool TryGetEntry(BindingExpressionBase binding, [NotNullWhen(true)] out FailedBindingDetails? bindingFailedDetails)
        {
            bindingFailedDetails = null;

            var entry = this.failedBindings.FirstOrDefault(x => x.Binding.TryGetTarget(out var keyBinding) && ReferenceEquals(keyBinding, binding));

            if (entry is not null)
            {
                bindingFailedDetails = entry;
                return true;
            }

            return false;
        }
#endif
    }

#if NET50
    [PublicAPI]
    public class FailedBindingDetails
    {
        public FailedBindingDetails(WeakReference<BindingExpressionBase> binding, TraceEventType eventType, int code, string message, params object[]? parameters)
        {
            this.Binding = binding;
            this.EventType = eventType;
            this.Code = code;
            this.Message = message;
            this.Parameters = parameters ?? Array.Empty<object>();
        }

        public WeakReference<BindingExpressionBase> Binding { get; }

        public TraceEventType EventType { get; }

        public int Code { get; }

        public string Message { get; }

#pragma warning disable CA1819
        public object[] Parameters { get; }
#pragma warning restore CA1819
    }
#endif
}