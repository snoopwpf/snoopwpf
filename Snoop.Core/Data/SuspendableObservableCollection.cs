namespace Snoop.Data;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

#pragma warning disable CA1001

public class SuspendableObservableCollection<T> : ObservableCollection<T>
{
    private Suspender? suspender;

    public SuspendableObservableCollection()
    {
    }

    public SuspendableObservableCollection(IEnumerable<T> collection)
        : base(collection)
    {
    }

    public SuspendableObservableCollection(List<T> list)
        : base(list)
    {
    }

    public IDisposable SuspendNotifications()
    {
        if (this.suspender is null)
        {
            this.suspender = new Suspender(this);
        }

        return this.suspender;
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (this.suspender is null)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (this.suspender is null)
        {
            base.OnPropertyChanged(e);
        }
    }

    private class Suspender : IDisposable
    {
        private readonly SuspendableObservableCollection<T> suspendableObservableCollection;

        public Suspender(SuspendableObservableCollection<T> suspendableObservableCollection)
        {
            this.suspendableObservableCollection = suspendableObservableCollection;
        }

        public void Dispose()
        {
            this.suspendableObservableCollection.suspender = null;

            this.suspendableObservableCollection.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.suspendableObservableCollection.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            this.suspendableObservableCollection.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}