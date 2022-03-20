namespace Snoop.Data;

#pragma warning disable CA2225

using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;

public class ResourceDictionaryWrapper
{
    public ResourceDictionaryWrapper(ResourceDictionary resourceDictionary, string? origin = null)
    {
        this.ResourceDictionary = resourceDictionary;
        this.Origin = origin ?? "resources";
    }

    public ResourceDictionary ResourceDictionary { get; }

    public string Source => this.ResourceDictionary.Source?.ToString() ?? "runtime dictionary";

    public string Origin { get; }

    public ICollection Keys => this.ResourceDictionary.Keys;

    public Collection<ResourceDictionary> MergedDictionaries => this.ResourceDictionary.MergedDictionaries;

    public static implicit operator ResourceDictionary(ResourceDictionaryWrapper wrapper)
    {
        return wrapper.ResourceDictionary;
    }

    public static implicit operator ResourceDictionaryWrapper(ResourceDictionary dictionary)
    {
        return new ResourceDictionaryWrapper(dictionary);
    }
}