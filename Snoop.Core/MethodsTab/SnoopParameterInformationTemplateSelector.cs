// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Snoop.MethodsTab
{
    public class SnoopParameterInformationTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            if (element == null)
                return null;

            SnoopParameterInformation snoopParameterInfo = item as SnoopParameterInformation;
            //if (snoopParameterInfo.TypeConverter.GetType() == typeof(TypeConverter))
            if (snoopParameterInfo.IsEnum || snoopParameterInfo.ParameterType.Equals(typeof(bool)))
            {
                return element.FindResource("EnumParameterTemplate") as DataTemplate;
            }
            if (snoopParameterInfo.ParameterType.Equals(typeof(DependencyProperty)))
            {
                return element.FindResource("DependencyPropertyTemplate") as DataTemplate;
            }
            if (snoopParameterInfo.IsCustom)
            {
                return element.FindResource("UnknownParameterTemplate") as DataTemplate;
            }
            else
            {
                return element.FindResource("DefaultParameterTemplate") as DataTemplate;
            }
        }
    }
}
