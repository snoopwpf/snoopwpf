namespace Snoop.Windows
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Snoop.Infrastructure;

    public class SnoopBaseWindow : Window
    {
        public SnoopBaseWindow()
        {
            this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;
            this.SnapsToDevicePixels = true;
            this.Icon = new BitmapImage(new Uri("pack://application:,,,/Snoop.Core;component/Snoop.ico"));

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
        }

        /// <inheritdoc />
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource is not null)
            {
                InputManager.Current.PushMenuMode(presentationSource);
            }
        }

        /// <inheritdoc />
        protected override void OnDeactivated(EventArgs e)
        {
            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource is not null)
            {
                InputManager.Current.PopMenuMode(presentationSource);
            }

            base.OnDeactivated(e);
        }
    }
}