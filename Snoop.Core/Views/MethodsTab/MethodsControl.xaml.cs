// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Snoop.Converters;
using Snoop.Windows;

public partial class MethodsControl
{
    private SnoopMethodInformation? previousMethodInformation;

    public MethodsControl()
    {
        this.InitializeComponent();
        DependencyPropertyDescriptor.FromProperty(RootTargetProperty, typeof(MethodsControl)).AddValueChanged(this, this.RootTargetChanged);

        //DependencyPropertyDescriptor.FromProperty(TargetProperty, typeof(MethodsControl)).AddValueChanged(this, OnTargetChanged);
        DependencyPropertyDescriptor.FromProperty(Selector.SelectedValueProperty, typeof(ComboBox)).AddValueChanged(this.comboBoxMethods, this.ComboBoxMethodChanged);
        DependencyPropertyDescriptor.FromProperty(IsSelectedProperty, typeof(MethodsControl)).AddValueChanged(this, this.IsSelectedChanged);

        this.checkBoxUseDataContext.Checked += this.CheckBoxUseDataContext_Checked;
        this.checkBoxUseDataContext.Unchecked += this.CheckBoxUseDataContext_Unchecked;
    }

    private void CheckBoxUseDataContext_Unchecked(object sender, RoutedEventArgs e)
    {
        this.ProcessCheckedProperty();
    }

    private void CheckBoxUseDataContext_Checked(object sender, RoutedEventArgs e)
    {
        this.ProcessCheckedProperty();
    }

    private void ProcessCheckedProperty()
    {
        if (!this.IsSelected
            || !this.checkBoxUseDataContext.IsChecked.HasValue
            || !(this.RootTarget is FrameworkElement))
        {
            return;
        }

        this.SetTargetToRootTarget();
    }

    private void SetTargetToRootTarget()
    {
        if (this.checkBoxUseDataContext.IsChecked == true
            && (this.RootTarget as FrameworkElement)?.DataContext is not null)
        {
            this.Target = ((FrameworkElement)this.RootTarget).DataContext;
        }
        else
        {
            this.Target = this.RootTarget;
        }
    }

    private void IsSelectedChanged(object? sender, EventArgs args)
    {
        if (this.IsSelected)
        {
            //this.Target = this.RootTarget;
            this.SetTargetToRootTarget();
        }
    }

    public object? RootTarget
    {
        get { return this.GetValue(RootTargetProperty); }
        set { this.SetValue(RootTargetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for RootTarget.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RootTargetProperty =
        DependencyProperty.Register(nameof(RootTarget), typeof(object), typeof(MethodsControl), new UIPropertyMetadata(null));

    private void RootTargetChanged(object? sender, EventArgs e)
    {
        if (this.IsSelected)
        {
            this.checkBoxUseDataContext.IsEnabled = (this.RootTarget as FrameworkElement)?.DataContext is not null;
            this.SetTargetToRootTarget();
        }
    }

    public bool IsSelected
    {
        get { return (bool)this.GetValue(IsSelectedProperty); }
        set { this.SetValue(IsSelectedProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(MethodsControl), new UIPropertyMetadata(false));

    private static void OnTargetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != e.OldValue)
        {
            var methodsControl = (MethodsControl)sender;

            methodsControl.EnableOrDisableDataContextCheckbox();

            var methodInfos = GetMethodInfos(methodsControl.Target);
            methodsControl.comboBoxMethods.ItemsSource = methodInfos;

            methodsControl.resultProperties.Visibility = Visibility.Collapsed;
            methodsControl.resultStringContainer.Visibility = Visibility.Collapsed;
            methodsControl.parametersContainer.Visibility = Visibility.Collapsed;

            //if this target has the previous method info, set it
            for (var i = 0; i < methodInfos.Count && methodsControl.previousMethodInformation is not null; i++)
            {
                var methodInfo = methodInfos[i];
                if (methodInfo.Equals(methodsControl.previousMethodInformation))
                {
                    methodsControl.comboBoxMethods.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void EnableOrDisableDataContextCheckbox()
    {
        if (this.checkBoxUseDataContext.IsChecked.HasValue && this.checkBoxUseDataContext.IsChecked.Value)
        {
            return;
        }

        if ((this.Target as FrameworkElement)?.DataContext is null)
        {
            this.checkBoxUseDataContext.IsEnabled = false;
        }
        else
        {
            this.checkBoxUseDataContext.IsEnabled = true;
        }
    }

    private void ComboBoxMethodChanged(object? sender, EventArgs e)
    {
        var selectedMethod = this.comboBoxMethods.SelectedValue as SnoopMethodInformation;
        if (selectedMethod is null
            || this.Target is null)
        {
            return;
        }

        var parameters = selectedMethod.GetParameters(this.Target.GetType());
        this.itemsControlParameters.ItemsSource = parameters;

        this.parametersContainer.Visibility = parameters.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        this.resultProperties.Visibility = this.resultStringContainer.Visibility = Visibility.Collapsed;

        this.previousMethodInformation = selectedMethod;
    }

    public object? Target
    {
        get { return this.GetValue(TargetProperty); }
        set { this.SetValue(TargetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(object), typeof(MethodsControl), new UIPropertyMetadata(OnTargetChanged));

    private void InvokeMethodClick(object? sender, RoutedEventArgs e)
    {
        var selectedMethod = this.comboBoxMethods.SelectedValue as SnoopMethodInformation;
        if (selectedMethod is null)
        {
            return;
        }

        var parameters = new object[this.itemsControlParameters.Items.Count];

        if (!this.TryToCreateParameters(parameters))
        {
            return;
        }

        this.TryToInvokeMethod(selectedMethod, parameters);
    }

    private bool TryToCreateParameters(object?[] parameters)
    {
        try
        {
            for (var index = 0; index < this.itemsControlParameters.Items.Count; index++)
            {
                if (this.itemsControlParameters.Items[index] is not SnoopParameterInformation paramInfo)
                {
                    return false;
                }

                if (paramInfo.ParameterType == typeof(DependencyProperty))
                {
                    var valuePair = paramInfo.ParameterValue as DependencyPropertyNameValuePair;
                    parameters[index] = valuePair?.DependencyProperty;
                }
                else if (paramInfo.ParameterValue is null
                         || paramInfo.ParameterType.Type.IsInstanceOfType(paramInfo.ParameterValue))
                {
                    parameters[index] = paramInfo.ParameterValue;
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);
                    parameters[index] = converter.ConvertFrom(paramInfo.ParameterValue);
                }
            }

            return true;
        }
        catch (Exception exception)
        {
            ErrorDialog.ShowDialog(exception, "Error creating parameter", exceptionAlreadyHandled: true);
            return false;
        }
    }

    private void TryToInvokeMethod(SnoopMethodInformation selectedMethod, object[] parameters)
    {
        try
        {
            var returnValue = selectedMethod.MethodInfo.Invoke(this.Target, parameters);

            if (returnValue is null)
            {
                this.SetNullReturnType(selectedMethod);
                return;
            }
            else
            {
                this.resultStringContainer.Visibility = this.textBlockResult.Visibility = this.textBlockResultLabel.Visibility = Visibility.Visible;
            }

            this.textBlockResultLabel.Text = "Result as string: ";
            this.textBlockResult.Text = returnValue.ToString();

            var properties = returnValue.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            //var properties = PropertyInformation.GetAllProperties(returnValue, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });

            if (properties.Length == 0)
            {
                this.resultProperties.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.resultProperties.Visibility = Visibility.Visible;
                this.propertyInspector.RootTarget = returnValue;
            }
        }
        catch (Exception exception)
        {
            ErrorDialog.ShowDialog(exception, $"Error invoking method '{selectedMethod.MethodName}'", exceptionAlreadyHandled: true);
        }
    }

    private void SetNullReturnType(SnoopMethodInformation selectedMethod)
    {
        if (selectedMethod.MethodInfo.ReturnType == typeof(void))
        {
            this.resultStringContainer.Visibility = this.resultProperties.Visibility = Visibility.Collapsed;
        }
        else
        {
            this.resultProperties.Visibility = Visibility.Collapsed;
            this.resultStringContainer.Visibility = Visibility.Visible;
            this.textBlockResult.Text = string.Empty;
            this.textBlockResultLabel.Text = "Method evaluated to null";
            this.textBlockResult.Visibility = Visibility.Collapsed;
        }
    }

    private static IList<SnoopMethodInformation> GetMethodInfos(object? o)
    {
        if (o is null)
        {
            return new ObservableCollection<SnoopMethodInformation>();
        }

        var t = o.GetType();
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod);

        var methodsToReturn = new List<SnoopMethodInformation>();

        foreach (var method in methods)
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            var info = new SnoopMethodInformation(method);

            methodsToReturn.Add(info);
        }

        methodsToReturn.Sort();

        return methodsToReturn;
    }

    private void ChangeTarget_Click(object? sender, RoutedEventArgs e)
    {
        if (this.RootTarget is null)
        {
            return;
        }

        var paramCreator = new ParameterCreator
        {
            TextBlockDescription =
            {
                Text = "Delve into the new desired target by double-clicking on the property. Clicking OK will select the currently delved property to be the new target."
            },
            Title = "Change Target",
            RootTarget = this.RootTarget
        };

        paramCreator.ShowDialogEx(this);

        if (paramCreator.DialogResult.HasValue && paramCreator.DialogResult.Value)
        {
            this.Target = paramCreator.SelectedTarget;
        }
    }
}