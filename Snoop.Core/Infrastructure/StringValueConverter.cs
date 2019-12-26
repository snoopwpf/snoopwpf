namespace Snoop.Infrastructure
{
    using System;
    using System.ComponentModel;

    public class StringValueConverter
    {
        public static object ConvertFromString(Type targetType, string value)
        {
            if (targetType.IsAssignableFrom(typeof(string)))
            {
                return value;
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null)
            {
                try
                {
                    using (new InvariantThreadCultureScope())
                    {
                        return GetValueFromConverter(targetType, value, converter);
                    }
                }
                catch
                {
                    // If we land here the problem might have been related to the threads culture.
                    // If the user entered data that was culture specific, we try setting it again using the original culture and not an invariant.
                    try
                    {
                        return GetValueFromConverter(targetType, value, converter);
                    }
                    catch
                    {
                        // todo: How should we notify the user about failures?
                    }
                }
            }

            return null;
        }

        private static object GetValueFromConverter(Type targetType, string value, TypeConverter converter)
        {
            if (string.IsNullOrEmpty(value)
                && converter.CanConvertFrom(targetType) == false)
            {
                return null;
            }
            else
            {
                return converter.ConvertFrom(value);
            }
        }
    }
}
