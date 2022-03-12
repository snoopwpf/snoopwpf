namespace Snoop;

using System;
using System.Windows.Input;
using System.Xml.Serialization;
using Snoop.Core;
using Snoop.Data;
using Snoop.Infrastructure;

[Serializable]
public sealed class Settings : SettingsBase<Settings>
{
    private static readonly XmlSerializer serializer = new(typeof(Settings));

    public static Settings Default { get; } = new Settings().Load();

    public Settings()
    {
        this.SettingsFile = SettingsHelper.GetSettingsFileForSnoop();
    }

    protected override XmlSerializer Serializer => serializer;

    public WINDOWPLACEMENT? AppChooserWindowPlacement { get; set; }

    public bool SetOwnerWindow { get; set; } = true;

    public MultipleAppDomainMode MultipleAppDomainMode { get; set; } = MultipleAppDomainMode.Ask;

    public MultipleDispatcherMode MultipleDispatcherMode { get; set; } = MultipleDispatcherMode.Ask;

    public KeyGestureEx? GlobalHotKey { get; set; } = new KeyGestureEx(Key.F12, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt);

    public string? ILSpyPath { get; set; } = "%path%";

    public bool EnableDiagnostics { get; set; } = true;

    protected override void UpdateWith(Settings settings)
    {
        this.SetOwnerWindow = settings.SetOwnerWindow;
        this.EnableDiagnostics = settings.EnableDiagnostics;

        this.MultipleDispatcherMode = settings.MultipleDispatcherMode;
        this.MultipleAppDomainMode = settings.MultipleAppDomainMode;

        this.AppChooserWindowPlacement = settings.AppChooserWindowPlacement;

        this.GlobalHotKey = settings.GlobalHotKey;

        this.ILSpyPath = settings.ILSpyPath;
    }
}