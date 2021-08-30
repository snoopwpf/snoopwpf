#if USE_WPF_BINDING_DIAG
namespace Snoop.Infrastructure.Diagnostics.Providers
{
    using System.Collections.Generic;

    public class BindingDiagnosticProvider : DiagnosticProvider
    {
        public override string Name => "Binding errors";

        public override string Description => "You should fix binding errors.";

        protected override IEnumerable<DiagnosticItem> GetGlobalDiagnosticItemsInternal()
        {
            foreach (var failedBinding in BindingDiagnosticHelper.Instance.FailedBindings)
            {
                yield return new(this, "Binding error", failedBinding.Messages, DiagnosticArea.Binding, DiagnosticLevel.Error);
            }
        }
    }
}
#endif