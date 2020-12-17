namespace Snoop.Extensions.Markup
{
    using System;
    using System.Windows.Markup;

    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type? enumType;

        public Type? EnumType
        {
            get => this.enumType;
            set
            {
                if (value != this.enumType)
                {
                    if (value != null)
                    {
                        var enumType = Nullable.GetUnderlyingType(value) ?? value;

                        if (!enumType.IsEnum)
                        {
                            throw new ArgumentException("Type must be for an Enum.");
                        }
                    }

                    this.enumType = value;
                }
            }
        }

        public EnumBindingSourceExtension()
        {
        }

        public EnumBindingSourceExtension(Type enumType)
        {
            this.EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (this.enumType == null)
            {
                throw new InvalidOperationException("The EnumType must be specified.");
            }

            var actualEnumType = Nullable.GetUnderlyingType(this.enumType) ?? this.enumType;
            var enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == this.enumType)
            {
                return enumValues;
            }

            var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }
}