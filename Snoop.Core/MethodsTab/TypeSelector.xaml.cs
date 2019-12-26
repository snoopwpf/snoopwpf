// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.MethodsTab
{
    using System;
    using System.Collections.Generic;

    public partial class TypeSelector : ITypeSelector
    {
        public TypeSelector()
        {
            InitializeComponent();

            this.Loaded += new System.Windows.RoutedEventHandler(TypeSelector_Loaded);
        }

        //TODO: MOVE SOMEWHERE ELSE. MACIEK
        public static List<Type> GetDerivedTypes(Type baseType)
        {
            List<Type> typesAssignable = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (baseType.IsAssignableFrom(type))
                    {
                        typesAssignable.Add(type);
                    }
                }
            }

            if (!baseType.IsAbstract)
            {
                typesAssignable.Add(baseType);
            }

            typesAssignable.Sort(new TypeComparerByName());

            return typesAssignable;
        }

        public List<Type> DerivedTypes
        {
            get;
            set;
        }

        private void TypeSelector_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            if (DerivedTypes == null)
            {
                this.DerivedTypes = GetDerivedTypes(this.BaseType);
            }

            this.comboBoxTypes.ItemsSource = DerivedTypes;
        }

        public Type BaseType { get; set; }

        public object Instance
        {
            get;
            private set;
        }

        private void buttonCreateInstance_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Instance = Activator.CreateInstance((Type)this.comboBoxTypes.SelectedItem);
            this.Close();
        }

        private void buttonCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }


}
