namespace Snoop.Core.Commands;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Snoop.Infrastructure;

public class InheritedValueSourceHelper : INotifyPropertyChanged
{
    public static readonly InheritedValueSourceHelper Instance = new();

    private static readonly PropertyInfo? inheritanceParentPropertyInfo = typeof(DependencyObject).GetProperty("InheritanceParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    public event PropertyChangedEventHandler? PropertyChanged;

    public InheritedValueSourceHelper()
    {
        this.Command = new RelayCommand(this.CommandExecute, this.CommandCanExecute);
    }

    public RelayCommand Command { get; }

    private void CommandExecute(object? obj)
    {
        if (obj is not PropertyInformation propertyInformation
            || propertyInformation.Target is not DependencyObject currentTarget
            || propertyInformation.DependencyProperty is null)
        {
            return;
        }

        //var currentTarget = propertyInformation.Target;
        var property = propertyInformation.DependencyProperty;

        var sources = GetInheritedValueSources(currentTarget, property);

        MessageBox.Show(string.Join(Environment.NewLine, sources.Select(x => $"{UIObjectNameHelper.GetName(x)}")));
    }

    private static ObservableCollection<DependencyObject> GetInheritedValueSources(DependencyObject? currentTarget, DependencyProperty property)
    {
        var sources = new ObservableCollection<DependencyObject>();

        if (currentTarget is not null)
        {
            sources.Add(currentTarget);
        }

        while ((currentTarget = inheritanceParentPropertyInfo?.GetValue(currentTarget) as DependencyObject) is not null)
        {
            sources.Add(currentTarget);

            if (DependencyPropertyHelper.GetValueSource(currentTarget, property).BaseValueSource != BaseValueSource.Inherited)
            {
                break;
            }
        }

        return sources;
    }

    private bool CommandCanExecute(object? obj)
    {
        return obj is PropertyInformation;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}