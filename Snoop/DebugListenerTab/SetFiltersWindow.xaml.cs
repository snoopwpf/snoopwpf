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
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace Snoop.DebugListenerTab
{
    /// <summary>
    /// Interaction logic for SetFiltersWindow.xaml
    /// </summary>
    public partial class SetFiltersWindow : Window
    {
        internal FiltersViewModel ViewModel
        {
            get
            {
                return this.DataContext as FiltersViewModel;
            }

        }


        private List<SnoopSingleFilter> initialFilters;
        public SetFiltersWindow(FiltersViewModel viewModel)
        {
            this.DataContext = viewModel;

            InitializeComponent();
            initialFilters = MakeDeepCopyOfFilters(this.ViewModel.Filters); 
            this.Closing += SetFiltersWindow_Closing;
        }

        void SetFiltersWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_setFilterClicked)
                return;

            var saveChanges = MessageBox.Show("Save changes?", "Changes", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            if (saveChanges)
                return;

            this.ViewModel.InitializeFilters(initialFilters);
        }


        private void buttonAddFilter_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.Filters.Add(new SnoopSingleFilter());
            ViewModel.AddFilter(new SnoopSingleFilter());
            //this.listBoxFilters.ScrollIntoView(this.listBoxFilters.ItemContainerGenerator.ContainerFromIndex(this.listBoxFilters.Items.Count - 1));

        }

        private void buttonRemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement == null)
                return;

            SnoopFilter filter = frameworkElement.DataContext as SnoopFilter;
            if (filter == null)
                return;

            ViewModel.RemoveFilter(filter);
        }
        private bool _setFilterClicked = false;
        private void buttonSetFilter_Click(object sender, RoutedEventArgs e)
        {
            SaveFiltersToSettings();

            //this.ViewModel.IsSet = true;
            this.ViewModel.SetIsSet();
            _setFilterClicked = true;
            this.Close();
        }

        private void SaveFiltersToSettings()
        {
            List<SnoopSingleFilter> singleFilters = new List<SnoopSingleFilter>();
            foreach (var filter in this.ViewModel.Filters)
            {
                if (filter is SnoopSingleFilter)
                    singleFilters.Add((SnoopSingleFilter)filter);
            }

            Properties.Settings.Default.SnoopDebugFilters = singleFilters.ToArray();
        }

        private void textBlockFilter_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Focus();
                this.listBoxFilters.ScrollIntoView(textBox);
            }
        }

        private void menuItemGroupFilters_Click(object sender, RoutedEventArgs e)
        {

            List<SnoopFilter> filtersToGroup = new List<SnoopFilter>();
            foreach (var item in this.listBoxFilters.SelectedItems)
            {
                var filter = item as SnoopFilter;
                if (filter == null)
                    continue;

                if (filter.SupportsGrouping)
                    filtersToGroup.Add(filter);
            }
            this.ViewModel.GroupFilters(filtersToGroup);
        }

        private void menuItemClearFilterGroups_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.ClearFilterGroups();
        }        

        private List<SnoopSingleFilter> MakeDeepCopyOfFilters(IEnumerable<SnoopFilter> filters)
        {
            List<SnoopSingleFilter> snoopSingleFilters = new List<SnoopSingleFilter>();

            foreach (var filter in filters)
            {
                var singleFilter = filter as SnoopSingleFilter;
                if (singleFilter == null)
                    continue;

                var newFilter = (SnoopSingleFilter)singleFilter.Clone();

                snoopSingleFilters.Add(newFilter);
            }

            return snoopSingleFilters;
        }

        //private SnoopSingleFilter MakeDeepCopyOfFilter(SnoopSingleFilter filter)
        //{
        //    try
        //    {
        //        BinaryFormatter formatter = new BinaryFormatter();
        //        var ms = new System.IO.MemoryStream();
        //        formatter.Serialize(ms, filter);
        //        SnoopSingleFilter deepCopy = (SnoopSingleFilter)formatter.Deserialize(ms);
        //        ms.Close();
        //        return deepCopy;
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}
    } 

}

