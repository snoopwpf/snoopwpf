namespace Snoop.Core;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Snoop.Infrastructure;

[Serializable]
public abstract class SettingsBase<T> : INotifyPropertyChanged
    where T : SettingsBase<T>, new()
{
    private string settingsFile = null!;

    protected abstract XmlSerializer Serializer { get; }

    [XmlIgnore]
    public string SettingsFile
    {
        get => this.settingsFile;
        set
        {
            if (value == this.settingsFile)
            {
                return;
            }

            this.settingsFile = value;
            this.OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Reset()
    {
        this.UpdateWith(new T());
    }

    public T Reload()
    {
        return this.Load();
    }

    protected abstract void UpdateWith(T settings);

    public T Load()
    {
        T? loadedSettings = null;

        if (File.Exists(this.SettingsFile) == false)
        {
            loadedSettings = new T();
        }
        else
        {
            try
            {
                using var stream = new FileStream(this.SettingsFile, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                loadedSettings = (T?)this.Serializer.Deserialize(stream);
            }
            catch (Exception exception)
            {
                LogHelper.WriteError(exception);
            }
        }

        this.UpdateWith(loadedSettings ?? new T());

        return (T)this;
    }

    public void Save()
    {
        LogHelper.WriteLine($"Writing settings to \"{this.SettingsFile}\"");

        var directory = Path.GetDirectoryName(this.SettingsFile);

        if (string.IsNullOrEmpty(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(this.SettingsFile, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        this.Serializer.Serialize(xmlWriter, this);
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}