// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Snoop.Infrastructure
{
    public static class ObservableCollectionExtensions
    {
        public static int BinarySearch<T>(this ObservableCollection<T> collection, T item)
            where T : IComparable
        {
            if (collection.Count == 0)
                return -1;


            return BinarySearch(collection, item, 0, collection.Count - 1);
        }

        public static int BinarySearch<T>(this ObservableCollection<T> collection, T item, Comparison<T> comparer)
        {
            if (collection.Count == 0)
                return -1;


            return BinarySearch(collection, item, 0, collection.Count - 1, comparer);
        }

        public static int BinarySearch<T>(ObservableCollection<T> collection, T item, int startIndex, int endIndex, Comparison<T> comparer)
        {
            if (startIndex > endIndex)
            {
                return -1;
            }

            if (startIndex == endIndex)
            {
                //if (collection[startIndex].CompareTo(item) != 0)
                if (comparer.Invoke(collection[startIndex], item) != 0)
                {
                    return -1;
                }
                else
                {
                    return startIndex;
                }
            }

            int middle = (startIndex + endIndex) / 2;

            //if (collection[middle].CompareTo(item) == 0)
            if (comparer.Invoke(collection[middle], item) == 0)
            {
                return middle;
            }

            //if (collection[middle].CompareTo(item) < 0)
            if (comparer.Invoke(collection[middle], item) < 0)
            {
                return BinarySearch(collection, item, middle + 1, endIndex, comparer);
            }
            else
            {
                return BinarySearch(collection, item, startIndex, middle - 1, comparer);
            }
        }

        public static int BinarySearch<T>(ObservableCollection<T> collection, T item, int startIndex, int endIndex)
            where T : IComparable
        {
            if (startIndex > endIndex)
            {
                return -1;
            }

            if (startIndex == endIndex)
            {
                if (collection[startIndex].CompareTo(item) != 0)
                {
                    return -1;
                }
                else
                {
                    return startIndex;
                }
            }

            int middle = (startIndex + endIndex) / 2;

            if (collection[middle].CompareTo(item) == 0)
            {
                return middle;
            }

            if (collection[middle].CompareTo(item) < 0)
            {
                return BinarySearch(collection, item, middle + 1, endIndex);
            }
            else
            {
                return BinarySearch(collection, item, startIndex, middle - 1);
            }
        }

        public static void InsertInOrder<T>(this ObservableCollection<T> collection, T item, Comparison<T> comparison)
        {
            if (collection.Count == 0)
            {
                collection.Add(item);
            }
            else
            {
                Insert(collection, item, 0, collection.Count - 1, comparison);
            }
        }

        private static void Insert<T>(ObservableCollection<T> collection, T item, int startIndex, int endIndex, Comparison<T> comparison)
        {
            //if (collection[startIndex].CompareTo(item) >= 0)//collection[0] >= item
            if (comparison.Invoke(collection[startIndex], item) >= 0)
            {
                collection.Insert(startIndex, item);
                return;
            }

            //if (collection[endIndex].CompareTo(item) <= 0)//lastItem >= item.
            if (comparison.Invoke(collection[endIndex], item) <= 0)
            {
                collection.Add(item);
                return;
            }

            int middle = (startIndex + endIndex) / 2;

            //if (collection[middle].CompareTo(item) >= 0)//middle >= item
            if (comparison.Invoke(collection[middle], item) >= 0)
            {
                Insert(collection, item, startIndex, middle, comparison);
            }
            else
            {
                Insert(collection, item, middle + 1, endIndex, comparison);
            }
        }

        public static void InsertInOrder<T>(this ObservableCollection<T> collection, T item)
            where T : IComparable
        {
            if (collection.Count == 0)
            {
                collection.Add(item);
            }
            else
            {
                Insert(collection, item, 0, collection.Count - 1);
            }
        }

        private static void Insert<T>(ObservableCollection<T> collection, T item, int startIndex, int endIndex)
            where T : IComparable
        {
            if (collection[startIndex].CompareTo(item) >= 0)//collection[0] >= item
            {
                collection.Insert(startIndex, item);
                return;
            }

            if (collection[endIndex].CompareTo(item) <= 0)//lastItem >= item.
            {
                collection.Add(item);
                return;
            }

            int middle = (startIndex + endIndex) / 2;

            if (collection[middle].CompareTo(item) >= 0)//middle >= item
            {
                Insert(collection, item, startIndex, middle);
            }
            else
            {
                Insert(collection, item, middle + 1, endIndex);
            }
        }
    }
}
