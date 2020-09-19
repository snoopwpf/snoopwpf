// ReSharper disable once CheckNamespace
namespace Snoop.Data
{
    using System.Diagnostics;
    using System.IO;
    using System.Xml.Serialization;

    public sealed class TransientSettingsData
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(TransientSettingsData));

        public TransientSettingsData()
        {
            this.MultipleAppDomainMode = MultipleAppDomainMode.Ask;
            this.MultipleDispatcherMode = MultipleDispatcherMode.Ask;
            this.SetWindowOwner = true;
        }

        public static TransientSettingsData Current { get; private set; }

        public SnoopStartTarget StartTarget { get; set; } = SnoopStartTarget.SnoopUI;

        public MultipleAppDomainMode MultipleAppDomainMode { get; set; }

        public MultipleDispatcherMode MultipleDispatcherMode { get; set; }

        public bool SetWindowOwner { get; set; }

        public long TargetWindowHandle { get; set; }

        public string WriteToFile()
        {
            var settingsFile = Path.GetTempFileName();

            Trace.WriteLine($"Writing transient settings file to \"{settingsFile}\"");

            using (var stream = new FileStream(settingsFile, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }

            return settingsFile;
        }

        public static TransientSettingsData LoadCurrentIfRequired(string settingsFile)
        {
            if (Current != null)
            {
                return Current;
            }

            return LoadCurrent(settingsFile);
        }

        public static TransientSettingsData LoadCurrent(string settingsFile)
        {
            Trace.WriteLine($"Loading transient settings file from \"{settingsFile}\"");

            using (var stream = new FileStream(settingsFile, FileMode.Open))
            {
                return Current = (TransientSettingsData)serializer.Deserialize(stream);
            }
        }
    }

    public enum MultipleAppDomainMode
    {
        Ask = 0,
        AlwaysUse = 1,
        NeverUse = 2
    }

    public enum MultipleDispatcherMode
    {
        Ask = 0,
        AlwaysUse = 1,
        NeverUse = 2
    }

    public enum SnoopStartTarget
    {
        SnoopUI = 0,
        Zoomer = 1
    }
}