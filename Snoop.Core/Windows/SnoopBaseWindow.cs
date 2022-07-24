namespace Snoop.Windows;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Snoop.Core;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Helpers;

public class SnoopBaseWindow : Window
{
    public SnoopBaseWindow()
    {
        this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;
        this.SnapsToDevicePixels = true;
        this.Icon = new BitmapImage(new Uri("pack://application:,,,/Snoop.Core;component/Snoop.ico"));

        SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);

        this.SetValue(DefaultStyleKeyFixer.KeyFixerProperty, new object());
    }

    protected override void OnInitialized(EventArgs e)
    {
        {
            var resourceDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Snoop.Core;component/Icons.xaml", UriKind.Relative));
            Debug.Assert(resourceDictionary is not null, "Icons could not be loaded.");
            this.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        {
            var resourceDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Snoop.Core;component/Styles.xaml", UriKind.Relative));
            Debug.Assert(resourceDictionary is not null, "Styles could not be loaded.");
            this.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        ThemeManager.Current.ApplyTheme(Settings.Default.ThemeMode, this);

        this.Style = (Style)this.FindResource(typeof(Window));

        base.OnInitialized(e);
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
        this.PopMenuModeSafe();

        base.OnDeactivated(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        this.PopMenuModeSafe();

        SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);

        base.OnClosed(e);
    }

    private void PopMenuModeSafe()
    {
        var presentationSource = PresentationSource.FromVisual(this);
        if (presentationSource is not null)
        {
            try
            {
                InputManager.Current.PopMenuMode(presentationSource);
            }
            catch
            {
                // ignored because we might have already popped the menu mode
            }
        }
    }
}