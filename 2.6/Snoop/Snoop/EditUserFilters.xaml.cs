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
		#region Construction

		public EditUserFilters()
		{
			InitializeComponent();
			DataContext = this;
		}

		#endregion

		#region Properties


		private IEnumerable<PropertyFilterSet> _userFilters;
		public IEnumerable<PropertyFilterSet> UserFilters
		{
			[DebuggerStepThrough]
			get { return _userFilters; }
			set
			{
				if ( value != _userFilters )
				{
					_userFilters = value;
					NotifyPropertyChanged( "UserFilters" );
					ItemsSource = new ObservableCollection<PropertyFilterSet>( UserFilters );
				}
			}
		}

		private ObservableCollection<PropertyFilterSet> _itemsSource;
		public ObservableCollection<PropertyFilterSet> ItemsSource
		{
			[DebuggerStepThrough]
			get { return _itemsSource; }
			private set
			{
				if ( value != _itemsSource )
				{
					_itemsSource = value;
					NotifyPropertyChanged( "ItemsSource" );
				}
			}
		}

		#endregion

		#region Private

		private void OkHandler( object sender, RoutedEventArgs e )
		{
			DialogResult = true;
			Close();
		}

		private void CancelHandler( object sender, RoutedEventArgs e )
		{
			DialogResult = false;
			Close();
		}

		private void AddHandler( object sender, RoutedEventArgs e )
		{
			var newSet = new PropertyFilterSet()
			                           	{
			                           		DisplayName = "New Filter",
			                           		IsDefault = false,
			                           		IsEditCommand = false,
											Properties = new String[] { "prop1,prop2" },
			                           	};
			ItemsSource.Add( newSet );

			// select this new item
			int index = ItemsSource.IndexOf( newSet );
			if ( index >= 0 )
			{
				TheList.SelectedIndex = index;
			}
		}

		private void DeleteHandler( object sender, RoutedEventArgs e )
		{
			var selected = TheList.SelectedItem as PropertyFilterSet;
			if ( selected != null )
			{
				ItemsSource.Remove( selected );
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		protected void NotifyPropertyChanged( string propertyName )
		{
			if ( PropertyChanged != null )
				PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
		}
		
		private void SelectionChangedHandler( object sender, SelectionChangedEventArgs e )
		{
			SetButtonStates();
		}

		private void UpHandler( object sender, RoutedEventArgs e )
		{
			int index = TheList.SelectedIndex;
			if ( index <= 0 )
				return;

			var item = ItemsSource[index];
			ItemsSource.RemoveAt( index );
			ItemsSource.Insert( index - 1, item );

			// select the moved item
			TheList.SelectedIndex = index - 1;

		}

		private void DownHandler( object sender, RoutedEventArgs e )
		{
			int index = TheList.SelectedIndex;
			if ( index >= ItemsSource.Count - 1 )
				return;

			var item = ItemsSource[index];
			ItemsSource.RemoveAt( index );
			ItemsSource.Insert( index + 1, item );

			// select the moved item
			TheList.SelectedIndex = index + 1;
		}

		private void SetButtonStates()
		{
			MoveUp.IsEnabled = false;
			MoveDown.IsEnabled = false;
			DeleteItem.IsEnabled = false;

			int index = TheList.SelectedIndex;
			if ( index >= 0 )
			{
				MoveDown.IsEnabled = true;
				DeleteItem.IsEnabled = true;
			}

			if ( index > 0 )
				MoveUp.IsEnabled = true;

			if ( index == TheList.Items.Count - 1 )
				MoveDown.IsEnabled = false;
				
		}

		#endregion

	}
}
