// ReSharper disable once CheckNamespace
namespace Snoop;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

public static class ResourceDictionaryExtensions
{
    public static bool TryGetValue(this ResourceDictionary resourceDictionary, object? key, out object? item)
    {
        return resourceDictionary.TryGetValue(key, out item, out _);
    }

    public static bool TryGetValue(this ResourceDictionary resourceDictionary, object? key, out object? item, [NotNullWhen(false)] out Exception? exception)
    {
        item = null;
        exception = null;

        try
        {
            item = resourceDictionary[key];

            return true;
        }
        catch (Exception ex)
        {
            // Sometimes we can get an exception ... because the xaml you are Snoop(ing) is bad.
            // e.g. I got this once when I was Snoop(ing) some xaml that was referring to an image resource that was no longer there.
            // Wrong style inheritance like this also cause exceptions here:
            // <Style x:Key="BlahStyle" TargetType="{x:Type RichTextBox}"/>
            // <Style x:Key="BlahBlahStyle" BasedOn="{StaticResource BlahStyle}" TargetType="{x:Type TextBoxBase}"/>

            // We only get an exception once. The next time through the value just comes back null.

            exception = ex;

            return false;
        }
    }
}