// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Snoop.Infrastructure;

public class SnoopParameterInformation : DependencyObject
{
    private ICommand? createCustomParameterCommand;
    private ICommand? nullOutParameter;

    public TypeConverter? TypeConverter { get; }

    public BindableType DeclaringType { get; }

    public bool IsCustom => !this.IsEnum && this.TypeConverter?.GetType() == typeof(TypeConverter);

    public bool IsEnum => this.ParameterType.IsEnum;

    public ICommand CreateCustomParameterCommand
    {
        get
        {
            return this.createCustomParameterCommand ??= new RelayCommand(_ => this.CreateCustomParameter());
        }
    }

    public ICommand NullOutParameterCommand
    {
        get
        {
            return this.nullOutParameter ??= new RelayCommand(_ => this.ParameterValue = null);
        }
    }

    private static ITypeSelector GetTypeSelector(Type parameterType)
    {
        ITypeSelector typeSelector;
        if (parameterType == typeof(object))
        {
            typeSelector = new FullTypeSelector();
        }
        else
        {
            typeSelector = new TypeSelector { BaseType = parameterType };
            //typeSelector.BaseType = parameterType;
        }

        typeSelector.Title = "Choose the type to instantiate";
        typeSelector.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        return typeSelector;
    }

    public void CreateCustomParameter()
    {
        var paramCreator = new ParameterCreator
        {
            Title = "Create parameter",
            TextBlockDescription =
            {
                Text = "Modify the properties of the parameter. Press OK to finalize the parameter"
            }
        };

        if (this.ParameterValue is null)
        {
            var typeSelector = GetTypeSelector(this.ParameterType);
            typeSelector.ShowDialog();

            if (typeSelector.DialogResult != true)
            {
                return;
            }

            paramCreator.RootTarget = typeSelector.Instance;
        }
        else
        {
            paramCreator.RootTarget = this.ParameterValue;
        }

        paramCreator.ShowDialogEx(this);

        if (paramCreator.DialogResult.HasValue && paramCreator.DialogResult.Value)
        {
            this.ParameterValue = null; //To force a property changed
            this.ParameterValue = paramCreator.RootTarget;
        }
    }

    public SnoopParameterInformation(ParameterInfo parameterInfo, Type declaringType)
    {
        this.DeclaringType = declaringType;
        this.ParameterName = parameterInfo.Name!;
        this.ParameterType = parameterInfo.ParameterType;

        if (this.ParameterType.IsValueType)
        {
            this.ParameterValue = Activator.CreateInstance(this.ParameterType);
        }

        this.TypeConverter = TypeDescriptor.GetConverter(this.ParameterType);
    }

    public string ParameterName { get; }

    public BindableType ParameterType { get; }

    public object? ParameterValue
    {
        get => this.GetValue(ParameterValueProperty);
        set => this.SetValue(ParameterValueProperty, value);
    }

    // Using a DependencyProperty as the backing store for ParameterValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ParameterValueProperty =
        DependencyProperty.Register(nameof(ParameterValue), typeof(object), typeof(SnoopParameterInformation), new UIPropertyMetadata(null));
}