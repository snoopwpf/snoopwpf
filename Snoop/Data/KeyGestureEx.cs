namespace Snoop.Data;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Markup;

[Serializable]
[TypeConverter(typeof(KeyGestureExConverter))]
[ValueSerializer(typeof(KeyGestureExValueSerializer))]
public class KeyGestureEx : KeyGesture, IEquatable<KeyGestureEx>
{
    public KeyGestureEx()
        : this(Key.None)
    {
    }

    public KeyGestureEx(Key key)
        : base(key)
    {
    }

    public KeyGestureEx(Key key, ModifierKeys modifiers)
        : base(key, modifiers)
    {
    }

    public KeyGestureEx(Key key, ModifierKeys modifiers, string displayString)
        : base(key, modifiers, displayString)
    {
    }

    public override bool Matches(object? targetElement, InputEventArgs inputEventArgs)
    {
        if (inputEventArgs is not KeyEventArgs keyEventArgs)
        {
            return false;
        }

        if (IsDefinedKey(keyEventArgs.Key) == false)
        {
            return false;
        }

        var modifiers = this.Modifiers;

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            modifiers &= ~ModifierKeys.Windows;

            if (Keyboard.IsKeyDown(Key.LWin) == false
                && Keyboard.IsKeyDown(Key.RWin) == false)
            {
                return false;
            }
        }

        return this.Key == keyEventArgs.Key
               && modifiers == Keyboard.Modifiers;
    }

    public static implicit operator KeyGestureEx(string s)
    {
        return (KeyGestureEx)KeyGestureExConverter.Default.ConvertFromString(s)!;
    }

    public static implicit operator string(KeyGestureEx r)
    {
        return KeyGestureExConverter.Default.ConvertToString(r)!;
    }

    public override string ToString()
    {
        return KeyGestureExConverter.Default.ConvertToString(this) ?? string.Empty;
    }

    private static bool IsDefinedKey(Key key)
    {
        return key is >= Key.None and <= Key.OemClear;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not KeyGestureEx other)
        {
            return false;
        }

        return this.Equals(other);
    }

    public bool Equals(KeyGestureEx? other)
    {
        if (other is null)
        {
            return false;
        }

        return this.Key == other.Key
               && this.Modifiers == other.Modifiers;
    }

#if NETCOREAPP
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Key, this.Modifiers);
        }
#else
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Key.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Modifiers.GetHashCode();
            return hashCode;
        }
    }
#endif

    public static bool operator ==(KeyGestureEx? left, KeyGestureEx? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(KeyGestureEx? left, KeyGestureEx? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
///     KeyGestureEx - Converter class for converting between a string and the Type of a KeyGestureEx
/// </summary>
public class KeyGestureExConverter : TypeConverter
{
#pragma warning disable SA1310 // Field names should not contain underscore
    // ReSharper disable InconsistentNaming
    private const char MODIFIERS_DELIMITER = '+';
    internal const char DISPLAYSTRING_SEPARATOR = ',';
    // ReSharper restore InconsistentNaming
#pragma warning restore SA1310 // Field names should not contain underscore

    public static readonly KeyGestureExConverter Default = new();

    private static readonly KeyConverter keyConverter = new();
    private static readonly ModifierKeysConverter modifierKeysConverter = new();

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        // We can only handle string.
        if (sourceType == typeof(string))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
    {
        if (source is string stringValue)
        {
            var fullName = stringValue.Trim();
            if (fullName == string.Empty)
            {
                return new KeyGestureEx(Key.None);
            }

            string keyToken;
            string modifiersToken;
            string displayString;

            // break apart display string
            var index = fullName.IndexOf(DISPLAYSTRING_SEPARATOR);
            if (index >= 0)
            {
                displayString = fullName.Substring(index + 1).Trim();
                fullName = fullName.Substring(0, index).Trim();
            }
            else
            {
                displayString = string.Empty;
            }

            // break apart key and modifiers
            index = fullName.LastIndexOf(MODIFIERS_DELIMITER);
            if (index >= 0)
            {
                // modifiers exists
                modifiersToken = fullName.Substring(0, index);
                keyToken = fullName.Substring(index + 1);
            }
            else
            {
                modifiersToken = string.Empty;
                keyToken = fullName;
            }

            var resultKey = keyConverter.ConvertFrom(context, culture, keyToken);
            var modifiers = (ModifierKeys)modifierKeysConverter.ConvertFrom(context, culture, modifiersToken);

            return new KeyGestureEx((Key)resultKey, modifiers, displayString);
        }

        throw this.GetConvertFromException(source);
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object? value, Type destinationType)
    {
        if (destinationType is null)
        {
            throw new ArgumentNullException(nameof(destinationType));
        }

        if (destinationType == typeof(string))
        {
            if (value is not null)
            {
                if (value is KeyGestureEx keyGesture)
                {
                    if (keyGesture.Key == Key.None)
                    {
                        return string.Empty;
                    }

                    var strBinding = string.Empty;
                    var strKey = (string)keyConverter.ConvertTo(context, culture, keyGesture.Key, destinationType);
                    if (strKey != string.Empty)
                    {
                        strBinding += modifierKeysConverter.ConvertTo(context, culture, keyGesture.Modifiers, destinationType) as string;
                        if (strBinding != string.Empty)
                        {
                            strBinding += MODIFIERS_DELIMITER;
                        }

                        strBinding += strKey;

                        if (!string.IsNullOrEmpty(keyGesture.DisplayString))
                        {
                            strBinding += DISPLAYSTRING_SEPARATOR + keyGesture.DisplayString;
                        }
                    }

                    return strBinding;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        throw this.GetConvertToException(value, destinationType);
    }

    // Check for Valid enum, as any int can be casted to the enum.
    internal static bool IsDefinedKey(Key key)
    {
        return key is >= Key.None and <= Key.OemClear;
    }
}

public class KeyGestureExValueSerializer : KeyGestureValueSerializer
{
    public override string ConvertToString(object value, IValueSerializerContext context)
    {
        return KeyGestureExConverter.Default.ConvertToString(value);
    }

    public override object ConvertFromString(string value, IValueSerializerContext context)
    {
        return KeyGestureExConverter.Default.ConvertFromString(value);
    }
}