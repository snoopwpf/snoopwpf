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

    public static Settings Default { get; } = new Settings().Load();

    public Settings()
    {
        this.SettingsFile = SettingsHelper.GetSettingsFileForCurrentProcess();

        this.Reset();
    }

    protected override XmlSerializer Serializer => serializer;

    public bool ShowDefaults { get; set; } = true;

    public bool ShowPreviewer { get; set; } = true;

    public bool ClearAfterDelve { get; set; } = true;

    public WINDOWPLACEMENT? SnoopUIWindowPlacement { get; set; }

    public WINDOWPLACEMENT? ZoomerWindowPlacement { get; set; }

    public ObservableCollection<PropertyFilterSet> UserDefinedPropertyFilterSets { get; private set; } = new();

    public ObservableCollection<SnoopSingleFilter> SnoopDebugFilters { get; private set; } = new();

    public ObservableCollection<EventTrackerSettingsItem> EventTrackers { get; private set; } = new();

    public int MaximumTrackedEvents { get; set; } = 100;

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