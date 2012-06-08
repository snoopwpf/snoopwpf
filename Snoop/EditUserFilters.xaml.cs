// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Snoop
{
	public partial class EditUserFilters : Window, INotifyPropertyChanged
	{
		public EditUserFilters()
		{
			InitializeComponent();
			DataContext = this;
		}


		public IEnumerable<PropertyFilterSet> UserFilters
		{
			[DebuggerStepThrough]
			get { return _userFilters; }
			set
			{
				if (value != _userFilters)
				{
					_userFilters = value;
					NotifyPropertyChanged("UserFilters");
					ItemsSource = new ObservableCollection<PropertyFilterSet>(UserFilters);
				}
			}
		}
		private IEnumerable<PropertyFilterSet> _userFilters;

		public ObservableCollection<PropertyFilterSet> ItemsSource
		{
			[DebuggerStepThrough]
			get { return _itemsSource; }
			private set
			{
				if (value != _itemsSource)
				{
					_itemsSource = value;
					NotifyPropertyChanged("ItemsSource");
				}
			}
		}
		private ObservableCollection<PropertyFilterSet> _itemsSource;


		private void OkHandler(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
		private void CancelHandler(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void AddHandler(object sender, RoutedEventArgs e)
		{
			var newSet =
				new PropertyFilterSet()
				{
					DisplayName = "New Filter",
					IsDefault = false,
					IsEditCommand = false,
					Properties = new String[] { "prop1,prop2" },
				};
			ItemsSource.Add(newSet);

			// select this new item
			int index = ItemsSource.IndexOf(newSet);
			if (index >= 0)
			{
				filterSetList.SelectedIndex = index;
			}
		}
		private void DeleteHandler(object sender, RoutedEventArgs e)
		{
			var selected = filterSetList.SelectedItem as PropertyFilterSet;
			if (selected != null)
			{
				ItemsSource.Remove(selected);
			}
		}

		private void UpHandler(object sender, RoutedEventArgs e)
		{
			int index = filterSetList.SelectedIndex;
			if (index <= 0)
				return;

			var item = ItemsSource[index];
			ItemsSource.RemoveAt(index);
			ItemsSource.Insert(index - 1, item);

			// select the moved item
			filterSetList.SelectedIndex = index - 1;

		}
		private void DownHandler(object sender, RoutedEventArgs e)
		{
			int index = filterSetList.SelectedIndex;
			if (index >= ItemsSource.Count - 1)
				return;

			var item = ItemsSource[index];
			ItemsSource.RemoveAt(index);
			ItemsSource.Insert(index + 1, item);

			// select the moved item
			filterSetList.SelectedIndex = index + 1;
		}

		private void SelectionChangedHandler(object sender, SelectionChangedEventArgs e)
		{
			SetButtonStates();
		}


		private void SetButtonStates()
		{
			MoveUp.IsEnabled = false;
			MoveDown.IsEnabled = false;
			DeleteItem.IsEnabled = false;

			int index = filterSetList.SelectedIndex;
			if (index >= 0)
			{
				MoveDown.IsEnabled = true;
				DeleteItem.IsEnabled = true;
			}

			if (index > 0)
				MoveUp.IsEnabled = true;

			if (index == filterSetList.Items.Count - 1)
				MoveDown.IsEnabled = false;
		}


		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
