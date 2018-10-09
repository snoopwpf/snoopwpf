namespace Snoop.Data
{
    using System.IO;
    using System.Xml.Serialization;
    using Snoop.Properties;

    public sealed class TransientSettingsData
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(TransientSettingsData));

        public TransientSettingsData()
        {
            this.SetWindowOwner = true;
            this.MultipleDispatcherMode = MultipleDispatcherMode.Ask;
        }

        internal TransientSettingsData(Settings settings)
        {
            this.SetWindowOwner = settings.SetOwnerWindow;
            this.MultipleDispatcherMode = settings.MultipleDispatcherMode;
        }

        public static TransientSettingsData Current { get; private set; }

        public bool SetWindowOwner { get; set; }

        public MultipleDispatcherMode MultipleDispatcherMode { get; set; }

        public string WriteToFile()
        {
            var settingsFile = Path.GetTempFileName();

            using (var stream = new FileStream(settingsFile, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }

            return settingsFile;
        }

        public static TransientSettingsData LoadCurrent(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open))
            {
                return Current = (TransientSettingsData)serializer.Deserialize(stream);
            }
        }
    }

    public enum MultipleDispatcherMode
    {
        Ask,
        AlwaysUse,
        NeverUse
    }
}