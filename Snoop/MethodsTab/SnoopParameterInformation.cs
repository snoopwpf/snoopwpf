// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Windows.Input;
using System.ComponentModel;
using Snoop.Infrastructure;

namespace Snoop.MethodsTab
{
    public class SnoopParameterInformation : DependencyObject
    {

        private ParameterInfo _parameterInfo = null;
        private ICommand _createCustomParameterCommand = null;
        private ICommand _nullOutParameter = null;

        public TypeConverter TypeConverter
        {
            get;
            private set;
        }

        public Type DeclaringType
        {
            get;
            private set;
        }

        public bool IsCustom
        {
            get
            {
                return !this.IsEnum && (TypeConverter.GetType() == typeof(TypeConverter));
            }
        }

        public bool IsEnum
        {
            get
            {
                return this.ParameterType.IsEnum;
            }
        }

        public ICommand CreateCustomParameterCommand
        {
            get
            {                
                return _createCustomParameterCommand ?? (_createCustomParameterCommand = new RelayCommand(x => CreateCustomParameter()));
            }
        }

        public ICommand NullOutParameterCommand
        {
            get
            {
                return _nullOutParameter ?? (_nullOutParameter = new RelayCommand(x => this.ParameterValue = null));
            }
        }

        private static ITypeSelector GetTypeSelector(Type parameterType)
        {
            ITypeSelector typeSelector = null;
            if (parameterType.Equals(typeof(object)))
            {
                typeSelector = new FullTypeSelector();
            }
            else
            {
                typeSelector = new TypeSelector() { BaseType = parameterType };
                //typeSelector.BaseType = parameterType;
            }

            typeSelector.Title = "Choose the type to instantiate";
            typeSelector.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return typeSelector;
        }

        public void CreateCustomParameter()
        {
            var paramCreator = new ParameterCreator();
            paramCreator.Title = "Create parameter";
            paramCreator.TextBlockDescription.Text = "Modify the properties of the parameter. Press OK to finalize the parameter";

            if (this.ParameterValue == null)
            {
                var typeSelector = GetTypeSelector(ParameterType);
                typeSelector.ShowDialog();

                if (!typeSelector.DialogResult.Value)
                {
                    return;
                }
                paramCreator.RootTarget = typeSelector.Instance;
            }
            else
            {
                paramCreator.RootTarget = this.ParameterValue;
            }

            paramCreator.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            paramCreator.ShowDialog();

            if (paramCreator.DialogResult.HasValue && paramCreator.DialogResult.Value)
            {
                ParameterValue = null;//To force a property changed
                ParameterValue = paramCreator.RootTarget;
            }
        }

        public SnoopParameterInformation(ParameterInfo parameterInfo, Type declaringType)
        {
            _parameterInfo = parameterInfo;
            if (parameterInfo == null)
                return;

            this.DeclaringType = declaringType;
            this.ParameterName = parameterInfo.Name;
            this.ParameterType = parameterInfo.ParameterType;
            if (this.ParameterType.IsValueType)
            {
                this.ParameterValue = Activator.CreateInstance(this.ParameterType);
            }
            TypeConverter = TypeDescriptor.GetConverter(this.ParameterType);
        }

        public string ParameterName { get; set; }

        public Type ParameterType { get; set; }

        public object ParameterValue
        {
            get { return (object)GetValue(ParameterValueProperty); }
            set { SetValue(ParameterValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParameterValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParameterValueProperty =
            DependencyProperty.Register("ParameterValue", typeof(object), typeof(SnoopParameterInformation), new UIPropertyMetadata(null));


    }
}
