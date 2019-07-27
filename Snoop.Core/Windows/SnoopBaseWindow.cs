namespace Snoop.Core.Windows
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using Snoop.Infrastructure;

    public class SnoopBaseWindow : Window
    {
        public SnoopBaseWindow()
        {
            this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
        }

        /// <inheritdoc />
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null)
            {
                InputManager.Current.PushMenuMode(presentationSource);
            }
        }

        /// <inheritdoc />
        protected override void OnDeactivated(EventArgs e)
        {
            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null)
            {
                InputManager.Current.PopMenuMode(presentationSource);
            }

            base.OnDeactivated(e);
        }
    }
}