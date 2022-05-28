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

    private bool enableDiagnostics = true;
    private KeyGestureEx? globalHotKey = new(Key.F12, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt);
    private string? ilSpyPath = "%path%";
    private MultipleAppDomainMode multipleAppDomainMode = MultipleAppDomainMode.Ask;
    private MultipleDispatcherMode multipleDispatcherMode = MultipleDispatcherMode.Ask;
    private bool setOwnerWindow = true;

    public Settings()
    {
        this.SettingsFile = SettingsHelper.GetSettingsFileForSnoop();
    }

    public static Settings Default { get; } = new Settings().Load();

    protected override XmlSerializer Serializer => serializer;

    public WINDOWPLACEMENT? AppChooserWindowPlacement { get; set; }

    public bool SetOwnerWindow
    {
        get => this.setOwnerWindow;
        set
        {
            if (value == this.setOwnerWindow)
            {
                return;
            }

            this.setOwnerWindow = value;
            this.OnPropertyChanged();
        }
    }

    public MultipleAppDomainMode MultipleAppDomainMode
    {
        get => this.multipleAppDomainMode;
        set
        {
            if (value == this.multipleAppDomainMode)
            {
                return;
            }

            this.multipleAppDomainMode = value;
            this.OnPropertyChanged();
        }
    }

    public MultipleDispatcherMode MultipleDispatcherMode
    {
        get => this.multipleDispatcherMode;
        set
        {
            if (value == this.multipleDispatcherMode)
            {
                return;
            }

            this.multipleDispatcherMode = value;
            this.OnPropertyChanged();
        }
    }

    [XmlElement(Type = typeof(string))]
    public KeyGestureEx? GlobalHotKey
    {
        get => this.globalHotKey;
        set
        {
            if (Equals(value, this.globalHotKey))
            {
                return;
            }

            this.globalHotKey = value;
            this.OnPropertyChanged();
        }
    }

    public string? ILSpyPath
    {
        get => this.ilSpyPath;
        set
        {
            if (value == this.ilSpyPath)
            {
                return;
            }

            this.ilSpyPath = value;
            this.OnPropertyChanged();
        }
    }

    public bool EnableDiagnostics
    {
        get => this.enableDiagnostics;
        set
        {
            if (value == this.enableDiagnostics)
            {
                return;
            }

            this.enableDiagnostics = value;
            this.OnPropertyChanged();
        }
    }

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