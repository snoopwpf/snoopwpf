// ReSharper disable once CheckNamespace
namespace Snoop;

using System;
using System.Windows.Markup;

// ReSharper disable once UnusedMember.Global
[MarkupExtensionReturnType(typeof(Array))]
public class EnumBindingSourceExtension : MarkupExtension
{
    private Type? enumType;

    [ConstructorArgument("enumType")]
    public BindableType? EnumType
    {
        get => this.enumType;
        set
        {
            if (value != this.enumType)
            {
                if (value is not null)
                {
                    var underlyingType = Nullable.GetUnderlyingType(value) ?? value;

                    if (!underlyingType.IsEnum)
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
        if (this.enumType is null)
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