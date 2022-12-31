namespace Snoop.Infrastructure.Helpers;

using System;
using System.Reflection;

public static class ReflectionHelper
{
    public static bool TrySetProperty(Type? type, string propertyName, object value, object? instance = null)
    {
        var propertyInfo = type?.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | (instance is null ? BindingFlags.Static : BindingFlags.Instance));

        if (propertyInfo is null)
        {
            // todo: add tracing
            return false;
        }

        propertyInfo.SetValue(instance, value, null);
        return true;
    }

    public static bool TryGetField(Type? type, string propertyName, out object? value, object? instance = null)
    {
        var fieldInfo = type?.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | (instance is null ? BindingFlags.Static : BindingFlags.Instance));

        if (fieldInfo is null)
        {
            value = null;
            // todo: add tracing
            return false;
        }

        value = fieldInfo.GetValue(instance);
        return true;
    }

    public static bool TrySetField(Type? type, string propertyName, object value, object? instance = null)
    {
        var fieldInfo = type?.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | (instance is null ? BindingFlags.Static : BindingFlags.Instance));

        if (fieldInfo is null)
        {
            // todo: add tracing
            return false;
        }

        fieldInfo.SetValue(instance, value);
        return true;
    }
}