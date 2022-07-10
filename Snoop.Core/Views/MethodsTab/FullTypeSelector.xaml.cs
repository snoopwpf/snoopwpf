// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Interaction logic for FullTypeSelector.xaml
/// </summary>
public partial class FullTypeSelector : ITypeSelector
{
    public FullTypeSelector()
    {
        this.InitializeComponent();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var listAssemblies = new List<AssemblyNamePair>();
        foreach (var assembly in assemblies)
        {
            var namePair = new AssemblyNamePair(assembly);

            listAssemblies.Add(namePair);
        }

        listAssemblies.Sort();

        this.comboBoxAssemblies.ItemsSource = listAssemblies;
    }

    private void ComboBoxAssemblies_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var assembly = ((AssemblyNamePair)this.comboBoxAssemblies.SelectedItem).Assembly;

        var types = assembly.GetTypes();

        var typePairs = new List<TypeNamePair>();

        foreach (var type in types)
        {
            if (!type.IsPublic
                || type.IsAbstract)
            {
                continue;
            }

            var pair = new TypeNamePair(type);

            typePairs.Add(pair);
        }

        typePairs.Sort();

        this.comboBoxTypes.ItemsSource = typePairs;
    }

    private void ButtonCreateInstance_Click(object sender, RoutedEventArgs e)
    {
        var selectedType = ((TypeNamePair)this.comboBoxTypes.SelectedItem).Type;

        if (string.IsNullOrEmpty(this.textBoxConvertFrom.Text))
        {
            this.Instance = Activator.CreateInstance(selectedType);
        }
        else
        {
            var converter = TypeDescriptor.GetConverter(selectedType);
            this.Instance = converter.ConvertFrom(this.textBoxConvertFrom.Text);
        }

        this.DialogResult = true;

        this.Close();
    }

    public object? Instance
    {
        get;
        private set;
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;

        this.Close();
    }
}