// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Windows;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using Snoop.Infrastructure;

public partial class EditUserFilters : INotifyPropertyChanged
{
    private ObservableCollection<PropertyFilterSet>? itemsSource;
    private IEnumerable<PropertyFilterSet>? userFilters;

    public EditUserFilters()
    {
        this.InitializeComponent();

        this.SetButtonStates();
    }

    public IEnumerable<PropertyFilterSet>? UserFilters
    {
        get => this.userFilters;
        set
        {
            if (!ReferenceEquals(value, this.userFilters))
            {
                this.userFilters = value;
                this.OnPropertyChanged(nameof(this.UserFilters));
                this.ItemsSource = this.UserFilters is null
                    ? new ObservableCollection<PropertyFilterSet>()
                    : new ObservableCollection<PropertyFilterSet>(this.UserFilters);
            }
        }
    }

    public ObservableCollection<PropertyFilterSet>? ItemsSource
    {
        get => this.itemsSource;
        private set
        {
            if (!ReferenceEquals(value, this.itemsSource))
            {
                this.itemsSource = value;
                this.OnPropertyChanged(nameof(this.ItemsSource));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OkHandler(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    private void CancelHandler(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void AddHandler(object sender, RoutedEventArgs e)
    {
        if (this.ItemsSource is null)
        {
            return;
        }

        var newSet =
            new PropertyFilterSet
            {
                DisplayName = "New Filter",
                Properties = new[] { "prop1,prop2" }
            };
        this.ItemsSource.Add(newSet);

        // select this new item
        var index = this.ItemsSource.IndexOf(newSet);
        if (index >= 0)
        {
            this.filterSetList.SelectedIndex = index;
        }
    }

    private void DeleteHandler(object sender, RoutedEventArgs e)
    {
        if (this.ItemsSource is null)
        {
            return;
        }

        if (this.filterSetList.SelectedItem is PropertyFilterSet selected)
        {
            this.ItemsSource.Remove(selected);
        }
    }

    private void UpHandler(object sender, RoutedEventArgs e)
    {
        if (this.ItemsSource is null)
        {
            return;
        }

        var index = this.filterSetList.SelectedIndex;
        if (index <= 0)
        {
            return;
        }

        var item = this.ItemsSource[index];
        this.ItemsSource.RemoveAt(index);
        this.ItemsSource.Insert(index - 1, item);

        // select the moved item
        this.filterSetList.SelectedIndex = index - 1;
    }

    private void DownHandler(object sender, RoutedEventArgs e)
    {
        if (this.ItemsSource is null)
        {
            return;
        }

        var index = this.filterSetList.SelectedIndex;
        if (index >= this.ItemsSource.Count - 1)
        {
            return;
        }

        var item = this.ItemsSource[index];
        this.ItemsSource.RemoveAt(index);
        this.ItemsSource.Insert(index + 1, item);

        // select the moved item
        this.filterSetList.SelectedIndex = index + 1;
    }

    private void SelectionChangedHandler(object sender, SelectionChangedEventArgs e)
    {
        this.SetButtonStates();
    }

    private void SetButtonStates()
    {
        this.MoveUp.IsEnabled = false;
        this.MoveDown.IsEnabled = false;
        this.DeleteItem.IsEnabled = false;

        if (this.filterSetList.SelectedItem is PropertyFilterSet { IsReadOnly: true })
        {
            return;
        }

        var index = this.filterSetList.SelectedIndex;
        if (index >= 0)
        {
            this.MoveDown.IsEnabled = true;
            this.DeleteItem.IsEnabled = true;
        }

        if (index > 0)
        {
            this.MoveUp.IsEnabled = true;
        }

        if (index == this.filterSetList.Items.Count - 1)
        {
            this.MoveDown.IsEnabled = false;
        }
    }

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}