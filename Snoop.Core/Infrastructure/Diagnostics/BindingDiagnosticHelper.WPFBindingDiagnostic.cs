#if USE_WPF_BINDING_DIAG
namespace Snoop.Infrastructure.Diagnostics
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Windows.Data;
    using System.Windows.Diagnostics;
    using JetBrains.Annotations;
    using Snoop.Infrastructure.Helpers;

    public partial class BindingDiagnosticHelper
    {
        private readonly ObservableCollection<FailedBinding> failedBindings = new();

        public ReadOnlyObservableCollection<FailedBinding> FailedBindings { get; private set; } = null!;

        private void ActivateWPFBindingDiagnostic()
        {
            this.FailedBindings = new(this.failedBindings);

            // check if VisualDiagnostics is enabled. If not we can't do much...
            if (ReflectionHelper.TryGetField(typeof(VisualDiagnostics), "s_IsEnabled", out var value)
                && value is bool and true)
            {
                if (ReflectionHelper.TrySetProperty(typeof(BindingDiagnostics), "IsEnabled", true))
                {
                    this.IsActive = true;

                    // this call, or better the Refresh class inside the method, causes BindingDiagnostics to start working as we set IsEnabled on it via reflection
                    PresentationTraceSourcesHelper.RefreshAndEnsureRequiredLevel(forceRefresh: true);
                }
            }
        }

        private void ListenForWPFBindingDiagnostic()
        {
            // todo: when starting via snoop maybe we should set env var ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO = true
            BindingDiagnostics.BindingFailed += this.BindingDiagnostics_BindingFailed;
        }

        private void UnListenForWPFBindingDiagnostic()
        {
            BindingDiagnostics.BindingFailed -= this.BindingDiagnostics_BindingFailed;
        }

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
    }

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
}
#endif