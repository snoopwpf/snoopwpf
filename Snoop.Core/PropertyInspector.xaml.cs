// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop
{
    public partial class PropertyInspector : INotifyPropertyChanged
    {
        public static readonly RoutedCommand SnipXamlCommand = new RoutedCommand("SnipXaml", typeof(PropertyInspector));
        public static readonly RoutedCommand PopTargetCommand = new RoutedCommand("PopTarget", typeof(PropertyInspector));
        public static readonly RoutedCommand DelveCommand = new RoutedCommand();
        public static readonly RoutedCommand DelveBindingCommand = new RoutedCommand();
        public static readonly RoutedCommand DelveBindingExpressionCommand = new RoutedCommand();
        public static readonly RoutedCommand NavigateToAssemblyInExplorerCommand = new RoutedCommand("NavigateToAssemblyInExplorer", typeof(PropertyInspector));

        public PropertyInspector()
        {
            this.InitializeComponent();

            this.inspector = this.PropertyGrid;
            this.inspector.Filter = this.propertyFilter;

            this.CommandBindings.Add(new CommandBinding(SnipXamlCommand, this.HandleSnipXaml, this.CanSnipXaml));
            this.CommandBindings.Add(new CommandBinding(PopTargetCommand, this.HandlePopTarget, this.CanPopTarget));
            this.CommandBindings.Add(new CommandBinding(DelveCommand, this.HandleDelve, this.CanDelve));
            this.CommandBindings.Add(new CommandBinding(DelveBindingCommand, this.HandleDelveBinding, this.CanDelveBinding));
            this.CommandBindings.Add(new CommandBinding(DelveBindingExpressionCommand, this.HandleDelveBindingExpression, this.CanDelveBindingExpression));
            this.CommandBindings.Add(new CommandBinding(NavigateToAssemblyInExplorerCommand, this.HandleNavigateToAssemblyInExplorer, this.CanNavigateToAssemblyInExplorer));

            // watch for mouse "back" button
            this.MouseDown += new MouseButtonEventHandler(this.MouseDownHandler);
            this.KeyDown += new KeyEventHandler(this.PropertyInspector_KeyDown);

            this.checkBoxClearAfterDelve.Checked += (s, e) => Properties.Settings.Default.ClearAfterDelve = this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value;
            this.checkBoxClearAfterDelve.Unchecked += (s, e) => Properties.Settings.Default.ClearAfterDelve = this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value;

            this.checkBoxClearAfterDelve.IsChecked = Properties.Settings.Default.ClearAfterDelve;
        }

        public bool NameValueOnly
        {
            get
            {
                return this.nameValueOnly;
            }
            set
            {
                this.PropertyGrid.NameValueOnly = value;
            }
        }
        private readonly bool nameValueOnly = false;

        private void HandleSnipXaml(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var xaml = XamlWriter.Save(((PropertyInformation)e.Parameter).Value);
                ClipboardHelper.SetText(xaml);
                MessageBox.Show("Brush has been copied to the clipboard. You can paste it into your project.", "Brush copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                ErrorDialog.ShowExceptionMessageBox(exception);
            }
        }

        private void CanSnipXaml(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).Value is Brush)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
        }

        public object RootTarget
        {
            get { return this.GetValue(RootTargetProperty); }
            set { this.SetValue(RootTargetProperty, value); }
        }

        public static readonly DependencyProperty RootTargetProperty =
            DependencyProperty.Register
            (
                nameof(RootTarget),
                typeof(object),
                typeof(PropertyInspector),
                new PropertyMetadata(HandleRootTargetChanged)
            );

        private static void HandleRootTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inspector = (PropertyInspector)d;

            inspector.inspectStack.Clear();
            inspector.Target = e.NewValue;

            inspector.delvePathList.Clear();
            inspector.OnPropertyChanged(nameof(DelvePath));
            inspector.OnPropertyChanged(nameof(DelveType));

            inspector.targetToFilter.Clear();
        }

        public object Target
        {
            get { return this.GetValue(TargetProperty); }
            set { this.SetValue(TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register
            (
                nameof(Target),
                typeof(object),
                typeof(PropertyInspector),
                new PropertyMetadata(HandleTargetChanged)
            );

        private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inspector = (PropertyInspector)d;
            inspector.OnPropertyChanged(nameof(Type));

            if (e.NewValue != null)
            {
                inspector.inspectStack.Add(e.NewValue);
            }

            if (inspector.lastRootTarget == inspector.RootTarget && e.OldValue != null && e.NewValue != null && inspector.checkBoxClearAfterDelve.IsChecked.HasValue && inspector.checkBoxClearAfterDelve.IsChecked.Value)
            {
                inspector.targetToFilter[e.OldValue.GetType()] = inspector.PropertiesFilter.Text;
                inspector.targetToFilter.TryGetValue(e.NewValue.GetType(), out var text);
                inspector.PropertiesFilter.Text = text ?? string.Empty;
            }
            inspector.lastRootTarget = inspector.RootTarget;
        }

        private string GetCurrentDelvePath(Type rootTargetType)
        {
            var delvePath = new StringBuilder(rootTargetType.Name);

            foreach (var propInfo in this.delvePathList)
            {
                int collectionIndex;
                if ((collectionIndex = propInfo.CollectionIndex()) >= 0)
                {
                    delvePath.Append(string.Format("[{0}]", collectionIndex));
                }
                else
                {
                    delvePath.Append(string.Format(".{0}", propInfo.DisplayName));
                }
            }

            return delvePath.ToString();
        }

        private Type GetCurrentDelveType(Type rootTargetType)
        {
            if (this.delvePathList.Count > 0)
            {
                var lastDelveEntry = this.delvePathList.Last();

                if (lastDelveEntry.Value is ISkipDelve skipDelve
                    && skipDelve.NextValue != null
                    && skipDelve.NextValueType != null)
                {
                    return skipDelve.NextValueType; //we want to make this "future friendly", so we take into account that the string value of the property type may change.
                }
                else if (lastDelveEntry.Value != null)
                {
                    return lastDelveEntry.Value.GetType();
                }
                else
                {
                    return lastDelveEntry.PropertyType;
                }
            }
            else if (this.delvePathList.Count == 0)
            {
                return rootTargetType;
            }

            return null;
        }

        /// <summary>
        /// Delve Path
        /// </summary>
        public string DelvePath
        {
            get
            {
                if (this.RootTarget == null)
                {
                    return "object is NULL";
                }

                var rootTargetType = this.RootTarget.GetType();
                var delvePath = this.GetCurrentDelvePath(rootTargetType);

                return delvePath;
            }
        }

        public Type DelveType
        {
            get
            {
                if (this.RootTarget == null)
                {
                    return null;
                }

                var rootTargetType = this.RootTarget.GetType();
                return this.GetCurrentDelveType(rootTargetType);
            }
        }

        public Type Type
        {
            get
            {
                if (this.Target != null)
                {
                    return this.Target.GetType();
                }

                return null;
            }
        }

        public void PushTarget(object target)
        {
            this.Target = target;
        }
        public void SetTarget(object target)
        {
            this.inspectStack.Clear();
            this.Target = target;
        }

        private void HandlePopTarget(object sender, ExecutedRoutedEventArgs e)
        {
            this.PopTarget();
        }

        private void PopTarget()
        {
            if (this.inspectStack.Count > 1)
            {
                this.Target = this.inspectStack[this.inspectStack.Count - 2];
                this.inspectStack.RemoveAt(this.inspectStack.Count - 2);
                this.inspectStack.RemoveAt(this.inspectStack.Count - 2);

                if (this.delvePathList.Count > 0)
                {
                    this.delvePathList.RemoveAt(this.delvePathList.Count - 1);
                    this.OnPropertyChanged(nameof(this.DelvePath));
                    this.OnPropertyChanged(nameof(this.DelveType));
                }
            }
        }
        private void CanPopTarget(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.inspectStack.Count > 1)
            {
                e.Handled = true;
                e.CanExecute = true;
            }
        }

        private object GetRealTarget(object target)
        {
            var skipDelve = target as ISkipDelve;
            if (skipDelve != null)
            {
                return skipDelve.NextValue;
            }
            return target;
        }

        private void HandleDelve(object sender, ExecutedRoutedEventArgs e)
        {
            var realTarget = this.GetRealTarget(((PropertyInformation)e.Parameter).Value);

            if (realTarget != this.Target)
            {
                // top 'if' statement is the delve path.
                // we do this because without doing this, the delve path gets out of sync with the actual delves.
                // the reason for this is because PushTarget sets the new target,
                // and if it's equal to the current (original) target, we won't raise the property-changed event,
                // and therefore, we don't add to our delveStack (the real one).

                this.delvePathList.Add((PropertyInformation)e.Parameter);
                this.OnPropertyChanged(nameof(this.DelvePath));
                this.OnPropertyChanged(nameof(this.DelveType));
            }

            if (this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value)
            {
                this.PropertiesFilter.Focus();
            }
            this.PushTarget(realTarget);
        }
        private void HandleDelveBinding(object sender, ExecutedRoutedEventArgs e)
        {
            this.PushTarget(((PropertyInformation)e.Parameter).Binding);
        }
        private void HandleDelveBindingExpression(object sender, ExecutedRoutedEventArgs e)
        {
            this.PushTarget(((PropertyInformation)e.Parameter).BindingExpression);
        }

        private void CanDelve(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).Value != null)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
        }
        private void CanDelveBinding(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).Binding != null)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
        }
        private void CanDelveBindingExpression(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).BindingExpression != null)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
        }

        private void HandleNavigateToAssemblyInExplorer(object sender, ExecutedRoutedEventArgs e)
        {
            var assembly = ((Type)e.Parameter).Assembly;
            var path = assembly.Location;

            if (string.IsNullOrEmpty(path))
            {
                path = assembly.CodeBase;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "explorer",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                Arguments = $"/e,/select,\"{path}\""
            };

            try
            {
                using (Process.Start(processStartInfo))
                {
                }
            }
            catch
            {
            }
        }

        private void CanNavigateToAssemblyInExplorer(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Type;
        }

        public PropertyFilter PropertyFilter
        {
            get { return this.propertyFilter; }
        }
        private readonly PropertyFilter propertyFilter = new PropertyFilter(string.Empty, true);

        public string StringFilter
        {
            get { return this.propertyFilter.FilterString; }
            set
            {
                this.propertyFilter.FilterString = value;

                this.inspector.Filter = this.propertyFilter;

                this.OnPropertyChanged(nameof(this.StringFilter));
            }
        }

        public bool ShowDefaults
        {
            get { return this.propertyFilter.ShowDefaults; }
            set
            {
                this.propertyFilter.ShowDefaults = value;

                this.inspector.Filter = this.propertyFilter;

                this.OnPropertyChanged(nameof(this.ShowDefaults));
            }
        }

        /// <summary>
        /// Looking for "browse back" mouse button.
        /// Pop properties context when clicked.
        /// </summary>
        private void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1)
            {
                this.PopTarget();
            }
        }
        private void PropertyInspector_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Left)
            {
                this.PopTarget();
            }
        }

        /// <summary>
        /// Hold the SelectedFilterSet in the PropertyFilter class, but track it here, so we know
        /// when to "refresh" the filtering with filterCall.Enqueue
        /// </summary>
        public PropertyFilterSet SelectedFilterSet
        {
            get { return this.propertyFilter.SelectedFilterSet ?? this.AllFilterSets[0]; }
            set
            {
                this.propertyFilter.SelectedFilterSet = value;
                this.OnPropertyChanged(nameof(this.SelectedFilterSet));

                if (value == null)
                {
                    return;
                }

                if (value.IsEditCommand)
                {
                    var dlg = new EditUserFilters { UserFilters = this.CopyFilterSets(this.defaultFilterSets.ToList().Concat(this.UserFilterSets)) };

                    var res = dlg.ShowDialogEx(this);

                    if (res.GetValueOrDefault())
                    {
                        // take the adjusted values from the dialog
                        this.UserFilterSets = CleanFiltersForUserFilters(dlg.ItemsSource);

                        Properties.Settings.Default.UserDefinedPropertyFilterSets = this.userFilterSets;
                        Properties.Settings.Default.Save();

                        this.SelectedFilterSet = null;
                        this.allFilterSets = null;

                        // trigger the UI to re-bind to the collection, so user sees changes they just made
                        this.OnPropertyChanged(nameof(this.AllFilterSets));
                    }
                }
                else
                {
                    this.inspector.Filter = this.propertyFilter;
                }

                this.OnPropertyChanged(nameof(this.SelectedFilterSet));
            }
        }

        /// <summary>
        /// Get or Set the collection of User filter sets.  These are the filters that are configurable by 
        /// the user, and serialized to/from app Settings.
        /// </summary>
        public PropertyFilterSet[] UserFilterSets
        {
            get
            {
                if (this.userFilterSets == null)
                {
                    var ret = new List<PropertyFilterSet>();

                    try
                    {
                        var userFilters = Properties.Settings.Default.UserDefinedPropertyFilterSets;

                        if (userFilters != null)
                        {
                            ret.AddRange(userFilters);
                        }
                    }
                    catch (Exception exception)
                    {
                        ErrorDialog.ShowDialog(exception, "Error reading user filters from settings. Using default filters.", exceptionAlreadyHandled: true);
                        ret.Clear();
                    }

                    this.userFilterSets = ret.ToArray();
                }
                return this.userFilterSets;
            }

            set
            {
                this.userFilterSets = value;
            }
        }

        /// <summary>
        /// Get the collection of "all" filter sets.  This is the UserFilterSets wrapped with 
        /// (Default) at the start and "Edit Filters..." at the end of the collection.
        /// This is the collection bound to in the UI 
        /// </summary>
        public PropertyFilterSet[] AllFilterSets
        {
            get
            {
                if (this.allFilterSets != null)
                {
                    return this.allFilterSets;
                }

                var ret = new List<PropertyFilterSet>(this.defaultFilterSets);
                ret.AddRange(this.UserFilterSets);

                // now add the "(Default)" and "Edit Filters..." filters for the ComboBox
                ret.Insert
                (
                    0,
                    new PropertyFilterSet
                    {
                        DisplayName = "(Default)",
                        IsDefault = true,
                        IsEditCommand = false,
                    }
                );
                ret.Add
                (
                    new PropertyFilterSet
                    {
                        DisplayName = "Edit Filters...",
                        IsDefault = false,
                        IsEditCommand = true,
                    }
                );

                this.allFilterSets = ret.ToArray();

                return this.allFilterSets;
            }
        }

        /// <summary>
        /// Make a deep copy of the filter collection.
        /// This is used when heading into the Edit dialog, so the user is editing a copy of the
        /// filters, in case they cancel the dialog - we dont want to alter their live collection.
        /// </summary>
        public PropertyFilterSet[] CopyFilterSets(IEnumerable<PropertyFilterSet> source)
        {
            return source.Select(x => x.Clone()).ToArray();
        }

        /// <summary>
        /// Cleanse the property names in each filter in the collection.
        /// This includes removing spaces from each one, and making them all lower case
        /// </summary>
        private static PropertyFilterSet[] CleanFiltersForUserFilters(ICollection<PropertyFilterSet> collection)
        {
            foreach (var filterItem in collection)
            {
                filterItem.Properties = filterItem.Properties.Select(s => s.ToLower().Trim()).ToArray();
            }
            return collection.Where(x => x.IsReadOnly == false).ToArray();
        }

        private readonly List<object> inspectStack = new List<object>();
        private PropertyFilterSet[] userFilterSets;
        private readonly List<PropertyInformation> delvePathList = new List<PropertyInformation>();

        private readonly Inspector inspector;
        private object lastRootTarget;
        private readonly Dictionary<object, string> targetToFilter = new Dictionary<object, string>();

        private PropertyFilterSet[] allFilterSets;

        private readonly PropertyFilterSet[] defaultFilterSets = 
        {
            new PropertyFilterSet
            {
                DisplayName = "Layout",
                IsReadOnly = true,
                Properties = new[]
                {
                    "width", "height", "actualwidth", "actualheight", 
                    "desiredsize",
                    "margin", "padding", 
                    "left", "top",
                    "horizontalalignment", "verticalalignment",
                    "horizontalcontentalignment", "verticalcontentalignment",
                }
            },
            new PropertyFilterSet
            {
                DisplayName = "Grid/Dock",
                IsReadOnly = true,
                Properties = new[]
                {
                    "grid", "dock"
                }
            },
            new PropertyFilterSet
            {
                DisplayName = "Color",
                IsReadOnly = true,
                Properties = new[]
                {
                    "color", "background", "foreground", "borderbrush", "fill", "stroke"
                }
            },
            new PropertyFilterSet
            {
                DisplayName = "ItemsControl",
                IsReadOnly = true,
                Properties = new[]
                {
                    "items", "itemssource", "selected"
                }
            }
        };

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
