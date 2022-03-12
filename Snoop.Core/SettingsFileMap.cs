namespace Snoop.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

[Serializable]
public class SettingsFileMap : SettingsBase<SettingsFileMap>
{
    public const string FileName = "SettingsFileMap.xml";

    public static SettingsFileMap Default { get; } = new SettingsFileMap().Load();

    public SettingsFileMap()
    {
        this.SettingsFile = Path.Combine(SettingsHelper.GetSnoopAppDataPath(), FileName);
    }

    public List<SettingsFileMapEntry> MappingEntries { get; set; } = new();

    protected override XmlSerializer Serializer { get; } = new(typeof(SettingsFileMap));

    protected override void UpdateWith(SettingsFileMap settings)
    {
        // ReSharper disable once ConstantNullCoalescingCondition
        this.MappingEntries = settings.MappingEntries ?? new List<SettingsFileMapEntry>();
    }

    public string GetSettingsFile(string processName, string processPath)
    {
        foreach (var mappingEntry in this.MappingEntries.OrderByDescending(x => x.Priority))
        {
            if (mappingEntry.IsMatch(processName, processPath))
            {
                return mappingEntry.SettingsFile;
            }
        }

        return string.Empty;
    }
}

[Serializable]
public class SettingsFileMapEntry
{
    public string ApplicationGlob { get; set; } = string.Empty;

    public string SettingsFile { get; set; } = string.Empty;

    public int Priority { get; set; }

    public bool IsMatch(string processName, string processPath)
    {
        if (Regex.IsMatch(processPath, this.ApplicationGlob)
            || Regex.IsMatch(processName, this.ApplicationGlob))
        {
            return true;
        }

        return false;
    }
}