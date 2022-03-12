// ReSharper disable once CheckNamespace

namespace Snoop.Core;

using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
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

    public Settings()
    {
        this.SettingsFile = SettingsHelper.GetSettingsFileForCurrentProcess();

        this.Reset();
    }

    public static Settings Default { get; } = new Settings().Load();

    protected override XmlSerializer Serializer => serializer;

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

    protected override void UpdateWith(Settings settings)
    {
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
}