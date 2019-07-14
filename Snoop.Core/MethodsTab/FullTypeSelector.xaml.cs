// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.ComponentModel;

namespace Snoop.MethodsTab
{
    /// <summary>
    /// Interaction logic for FullTypeSelector.xaml
    /// </summary>
    public partial class FullTypeSelector : ITypeSelector
    {
        public FullTypeSelector()
        {
            InitializeComponent();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<AssemblyNamePair> listAssemblies = new List<AssemblyNamePair>();
            foreach (var assembly in assemblies)
            {
                var namePair = new AssemblyNamePair();
                namePair.Name = assembly.FullName;
                namePair.Assembly = assembly;

                listAssemblies.Add(namePair);
            }

            listAssemblies.Sort();

            this.comboBoxAssemblies.ItemsSource = listAssemblies;


        }

        private void comboBoxAssemblies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var assembly = ((AssemblyNamePair)this.comboBoxAssemblies.SelectedItem).Assembly;

            var types = assembly.GetTypes();

            List<TypeNamePair> typePairs = new List<TypeNamePair>();

            foreach (var type in types)
            {
                if (!type.IsPublic || type.IsAbstract)
                    continue;

                var pair = new TypeNamePair();
                pair.Name = type.Name;
                pair.Type = type;

                typePairs.Add(pair);
            }

            typePairs.Sort();

            this.comboBoxTypes.ItemsSource = typePairs;
        }

        private void buttonCreateInstance_Click(object sender, RoutedEventArgs e)
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

        public object Instance
        {
            get;
            private set;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

            this.Close();
        }


    }

    //public class TypeNamePair : IComparable
    //{
    //    public string Name { get; set; }

    //    public Type Type { get; set; }

    //    public override string ToString()
    //    {
    //        return Name;
    //    }

    //    #region IComparable Members

    //    public int CompareTo(object obj)
    //    {
    //        return Name.CompareTo(((TypeNamePair)obj).Name);
    //    }

    //    #endregion
    //}

    //public class AssemblyNamePair : IComparable
    //{
    //    public string Name { get; set; }

    //    public Assembly Assembly { get; set; }

    //    public override string ToString()
    //    {
    //        return Name;
    //    }

    //    #region IComparable Members

    //    public int CompareTo(object obj)
    //    {
    //        return Name.CompareTo(((AssemblyNamePair)obj).Name);
    //    }

    //    #endregion
    //}
}
