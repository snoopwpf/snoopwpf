namespace Snoop.Converters
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;

    internal class DynamicResourceToValueConverter : IValueConverter
    {
        private readonly object target;

        public DynamicResourceToValueConverter(object target)
        {
            this.target = target;
        }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DynamicResourceExtension == false)
            {
                return value;
            }

            var resource = FindResource(this.target as FrameworkElement, ((DynamicResourceExtension)value).ResourceKey, null);

            if (resource != null)
            {
                return resource.Value;
            }

            return value;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static Resource FindResource(FrameworkElement source, object resourceKey, object value)
        {
            if (source == null)
            {
                return null;
            }

            if (resourceKey == null
                && value == null)
            {
                return null;
            }

            var resource = FindResource(source.Resources, resourceKey, value);

            if (resource == null)
            {
                var window = Window.GetWindow(source);

                if (window != null)
                {
                    resource = FindResource(window.Resources, resourceKey, value);
                }

                if (resource == null)
                {
                    if (Application.Current != null)
                    {
                        resource = FindResource(Application.Current.Resources, resourceKey, value);
                    }
                }
            }

            if (resource == null)
            {
                return new Resource(null, resourceKey, value);
            }

            return resource;
        }

        private static Resource FindResource(ResourceDictionary dictionary, object resourceKey, object value)
        {
            if (resourceKey != null)
            {
                if (dictionary.Contains(resourceKey))
                {
                    return new Resource(dictionary, resourceKey, dictionary[resourceKey]);
                }
            }
            else
            {
                foreach (var key in dictionary.Keys)
                {
                    if (dictionary[key] == value)
                    {
                        return new Resource(dictionary, key, value);
                    }
                }
            }

            foreach (var mergedDictionary in dictionary.MergedDictionaries.Reverse())
            {
                var resource = FindResource(mergedDictionary, resourceKey, value);
                if (resource != null)
                {
                    return resource;
                }
            }

            return null;
        }

        private class Resource
        {
            public Resource(ResourceDictionary resourceDictionary, object resourceKey, object value)
            {
                this.ResourceDictionary = resourceDictionary;
                this.ResourceKey = resourceKey;
                this.Value = value;
            }

            public ResourceDictionary ResourceDictionary { get; private set; }

            public object ResourceKey { get; private set; }

            public object Value { get; private set; }
        }
    }
}