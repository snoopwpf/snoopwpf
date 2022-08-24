// ReSharper disable once CheckNamespace

namespace Snoop.Core;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Snoop.Infrastructure;
using Snoop.Views.DebugListenerTab;

[Serializable]
public sealed class Settings : SettingsBase<Settings>
{
    private static readonly XmlSerializer serializer = new(typeof(Settings));

    private bool clearAfterDelve = true;
    private int maximumTrackedEvents = 100;
    private bool showDefaults = true;
    private bool showPreviewer;
    private bool isDefaultSettingsFile;
    private ThemeMode themeMode;

    public Settings()
    {
        this.SettingsFile = SettingsHelper.GetSettingsFileForCurrentApplication();
    }

    public static Settings Default { get; } = new Settings().Load();

    protected override XmlSerializer Serializer => serializer;

    [XmlIgnore]
    public bool IsDefaultSettingsFile
    {
        get => this.isDefaultSettingsFile;
        set
        {
            if (value == this.isDefaultSettingsFile)
            {
                return;
            }

            this.isDefaultSettingsFile = value;
            this.OnPropertyChanged();
        }
    }

    public bool ShowDefaults
    {
        get => this.showDefaults;
        set
        {
            if (value == this.showDefaults)
            {
                return;
            }

            this.showDefaults = value;
            this.OnPropertyChanged();
        }
    }

    public bool ShowPreviewer
    {
        get => this.showPreviewer;
        set
        {
            if (value == this.showPreviewer)
            {
                return;
            }

            this.showPreviewer = value;
            this.OnPropertyChanged();
        }
    }

    public bool ClearAfterDelve
    {
        get => this.clearAfterDelve;
        set
        {
            if (value == this.clearAfterDelve)
            {
                return;
            }

            this.clearAfterDelve = value;
            this.OnPropertyChanged();
        }
    }

    public WINDOWPLACEMENT? SnoopUIWindowPlacement { get; set; }

    public WINDOWPLACEMENT? ZoomerWindowPlacement { get; set; }

    public ObservableCollection<PropertyFilterSet> UserDefinedPropertyFilterSets { get; private set; } = new();

    public ObservableCollection<SnoopSingleFilter> SnoopDebugFilters { get; private set; } = new();

    public ObservableCollection<EventTrackerSettingsItem> EventTrackers { get; private set; } = new();

    public int MaximumTrackedEvents
    {
        get => this.maximumTrackedEvents;
        set
        {
            if (value == this.maximumTrackedEvents)
            {
                return;
            }

            this.maximumTrackedEvents = value;
            this.OnPropertyChanged();
        }
    }

    public ThemeMode ThemeMode
    {
        get => this.themeMode;
        set
        {
            this.themeMode = value;
            this.OnPropertyChanged();
            ThemeManager.Current.ApplyTheme(value);
        }
    }

    protected override void UpdateWith(Settings settings)
    {
        this.ThemeMode = settings.ThemeMode;
        this.ShowDefaults = settings.ShowDefaults;
        this.ShowPreviewer = settings.ShowPreviewer;
        this.ClearAfterDelve = settings.ClearAfterDelve;
        this.MaximumTrackedEvents = settings.MaximumTrackedEvents;

        this.SnoopUIWindowPlacement = settings.SnoopUIWindowPlacement;
        this.ZoomerWindowPlacement = settings.ZoomerWindowPlacement;

        this.UserDefinedPropertyFilterSets.UpdateWith(settings.UserDefinedPropertyFilterSets);
        this.SnoopDebugFilters.UpdateWith(settings.SnoopDebugFilters);
        this.EventTrackers.UpdateWith(settings.EventTrackers);
    }

    [NotifyPropertyChangedInvocator]
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(this.SettingsFile))
        {
            this.IsDefaultSettingsFile = Path.GetFileName(this.SettingsFile).Equals("DefaultSettings.xml", StringComparison.OrdinalIgnoreCase);
        }
    }
}

[PublicAPI]
public enum ThemeMode
{
    Auto = 0,
    Dark = 1,
    Light = 2
}