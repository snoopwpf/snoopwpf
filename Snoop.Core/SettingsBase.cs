namespace Snoop.Core;

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Snoop.Infrastructure;

[Serializable]
public abstract class SettingsBase<T>
    where T : SettingsBase<T>, new()
{
    protected abstract XmlSerializer Serializer { get; }

    [XmlIgnore]
    public string SettingsFile { get; set; } = null!;

    public void Reset()
    {
        this.UpdateWith(new T());
    }

    public void Reload()
    {
    }

    protected abstract void UpdateWith(T settings);

    public T Load()
    {
        if (File.Exists(this.SettingsFile) == false)
        {
            return (T)this;
        }

        try
        {
            using var stream = new FileStream(this.SettingsFile, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var settings = (T?)this.Serializer.Deserialize(stream);
            if (settings is not null)
            {
                this.UpdateWith(settings);
            }
        }
        catch (Exception exception)
        {
            LogHelper.WriteError(exception);
        }

        return (T)this;
    }

    public void Save()
    {
        LogHelper.WriteLine($"Writing settings to \"{this.SettingsFile}\"");

        // MemberInfo[] members = FormatterServices.GetSerializableMembers(this.GetType());
        // var data = FormatterServices.GetObjectData(this, members);

        using var stream = new FileStream(this.SettingsFile, FileMode.OpenOrCreate, FileAccess.Write);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        this.Serializer.Serialize(xmlWriter, this);
    }
}