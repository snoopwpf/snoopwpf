namespace Snoop.Infrastructure.Diagnostics
{
    using System;
    using System.Collections.Generic;
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
        private readonly ObservableCollection<FailedBinding> failedBindings = new();
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
                // this call, or better the Refresh class inside the method, causes BindingDiagnostics to start working as we set IsEnabled on it via reflection
                PresentationTraceSourcesHelper.RefreshAndEnsureInformationLevel(forceRefresh: true);

                // todo: when starting via snoop maybe we should set env var ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO = true
                BindingDiagnostics.BindingFailed += this.BindingDiagnostics_BindingFailed;
            }
        #endif
        }

        public bool IsActive { get; }

#if NET50
        public ReadOnlyObservableCollection<FailedBinding> FailedBindings { get; }
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

            var bindingExpressionBase = e.Binding;

            if (this.TryGetEntry(bindingExpressionBase, out var failedBinding) == false)
            {
                var weakReference = new WeakReference<BindingExpressionBase>(bindingExpressionBase);
                failedBinding = new FailedBinding(weakReference);
                this.failedBindings.Add(failedBinding);
            }

            {
                failedBinding.AddFailedBindingDetail(new FailedBindingDetail(failedBinding, e.EventType, e.Code, e.Message));
            }
        }
#endif

        public void TrySetBindingError(BindingExpressionBase bindingExpressionBase, DependencyObject dependencyObject, DependencyProperty dependencyProperty, Action<string> errorSetter)
        {
#if NET50
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
                    PresentationTraceSourcesHelper.EnsureInformationLevel();
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
            var binding = bindingExpressionBase.ParentBindingBase;
            BindingOperations.SetBinding(dependencyObject, dependencyProperty, binding);

            scopeGuard.Guard(); // Start listening

            // this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
            dependencyObject.RunInDispatcherAsync(
                () =>
                {
                    scopeGuard.Dispose();
                    errorSetter(builder.ToString());
                }, DispatcherPriority.ApplicationIdle);
        }

#if NET50
        public bool TryGetEntry(BindingExpressionBase bindingExpressionBase, [NotNullWhen(true)] out FailedBinding? bindingFailedDetails)
        {
            bindingFailedDetails = null;

            var entry = this.failedBindings.FirstOrDefault(x => x.BindingExpressionBase.TryGetTarget(out var keyBindingExpressionBase) && ReferenceEquals(keyBindingExpressionBase, bindingExpressionBase));

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
    public class FailedBinding
    {
        private readonly ObservableCollection<FailedBindingDetail> failedBindingDetails;
        private string? messages;

        public FailedBinding(WeakReference<BindingExpressionBase> bindingExpressionBase)
        {
            this.BindingExpressionBase = bindingExpressionBase;
            this.failedBindingDetails = new ObservableCollection<FailedBindingDetail>();
            this.FailedBindingDetails = new ReadOnlyObservableCollection<FailedBindingDetail>(this.failedBindingDetails);
        }

        public WeakReference<BindingExpressionBase> BindingExpressionBase { get; }

        public ReadOnlyObservableCollection<FailedBindingDetail> FailedBindingDetails { get; }

        public void AddFailedBindingDetail(FailedBindingDetail failedBindingDetail)
        {
            this.messages = null;
            this.failedBindingDetails.Add(failedBindingDetail);
        }

        public string Messages => this.messages ??= this.BuildMessages();

        private string BuildMessages()
        {
            var sb = new StringBuilder();

            foreach (var failedBindingDetail in this.failedBindingDetails)
            {
                sb.AppendFormat("{0} {1}: {2}", failedBindingDetail.EventType, failedBindingDetail.Code, failedBindingDetail.Message);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }

    [PublicAPI]
    public class FailedBindingDetail
    {
        public FailedBindingDetail(FailedBinding failedBinding, TraceEventType eventType, int code, string message, params object[]? parameters)
        {
            this.FailedBinding = failedBinding;
            this.EventType = eventType;
            this.Code = code;
            this.Message = message;
            this.Parameters = parameters ?? Array.Empty<object>();
        }

        public FailedBinding FailedBinding { get; }

        public TraceEventType EventType { get; }

        public int Code { get; }

        public string Message { get; }

#pragma warning disable CA1819
        public object[] Parameters { get; }
#pragma warning restore CA1819
    }
#endif
}