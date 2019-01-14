// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections;
using System.Reflection;
using Snoop.Infrastructure;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;

namespace Snoop
{
    public partial class PropertyInspector : INotifyPropertyChanged
    {
        public static readonly RoutedCommand SnipXamlCommand = new RoutedCommand("SnipXaml", typeof(PropertyInspector));
        public static readonly RoutedCommand PopTargetCommand = new RoutedCommand("PopTarget", typeof(PropertyInspector));
        public static readonly RoutedCommand DelveCommand = new RoutedCommand();
        public static readonly RoutedCommand DelveBindingCommand = new RoutedCommand();
        public static readonly RoutedCommand DelveBindingExpressionCommand = new RoutedCommand();

        public PropertyInspector()
        {
            propertyFilter.SelectedFilterSet = AllFilterSets[0];

            this.InitializeComponent();

            this.inspector = this.PropertyGrid;
            this.inspector.Filter = this.propertyFilter;

            this.CommandBindings.Add(new CommandBinding(PropertyInspector.SnipXamlCommand, this.HandleSnipXaml, this.CanSnipXaml));
            this.CommandBindings.Add(new CommandBinding(PropertyInspector.PopTargetCommand, this.HandlePopTarget, this.CanPopTarget));
            this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveCommand, this.HandleDelve, this.CanDelve));
            this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveBindingCommand, this.HandleDelveBinding, this.CanDelveBinding));
            this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveBindingExpressionCommand, this.HandleDelveBindingExpression, this.CanDelveBindingExpression));

            // watch for mouse "back" button
            this.MouseDown += new MouseButtonEventHandler(MouseDownHandler);
            this.KeyDown += new KeyEventHandler(PropertyInspector_KeyDown);

            this.checkBoxClearAfterDelve.Checked += (s, e) => Properties.Settings.Default.ClearAfterDelve = this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value;
            this.checkBoxClearAfterDelve.Unchecked += (s, e) => Properties.Settings.Default.ClearAfterDelve = this.checkBoxClearAfterDelve.IsChecked.HasValue && this.checkBoxClearAfterDelve.IsChecked.Value;

            this.checkBoxClearAfterDelve.IsChecked = Properties.Settings.Default.ClearAfterDelve;
        }

        public bool NameValueOnly
        {
            get
            {
                return _nameValueOnly;
            }
            set
            {
                this.PropertyGrid.NameValueOnly = value;
            }
        }
        private bool _nameValueOnly = false;

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
                e.CanExecute = true;
            e.Handled = true;
        }

        public object RootTarget
        {
            get { return this.GetValue(PropertyInspector.RootTargetProperty); }
            set { this.SetValue(PropertyInspector.RootTargetProperty, value); }
        }
        public static readonly DependencyProperty RootTargetProperty =
            DependencyProperty.Register
            (
                "RootTarget",
                typeof(object),
                typeof(PropertyInspector),
                new PropertyMetadata(PropertyInspector.HandleRootTargetChanged)
            );
        private static void HandleRootTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyInspector inspector = (PropertyInspector)d;

            inspector.inspectStack.Clear();
            inspector.Target = e.NewValue;

            inspector._delvePathList.Clear();
            inspector.OnPropertyChanged("DelvePath");

            inspector.targetToFilter.Clear();
        }

        public object Target
        {
            get { return this.GetValue(PropertyInspector.TargetProperty); }
            set { this.SetValue(PropertyInspector.TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register
            (
                "Target",
                typeof(object),
                typeof(PropertyInspector),
                new PropertyMetadata(PropertyInspector.HandleTargetChanged)
            );

        
        private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyInspector inspector = (PropertyInspector)d;
            inspector.OnPropertyChanged("Type");

            if (e.NewValue != null)
                inspector.inspectStack.Add(e.NewValue);

            if (inspector.lastRootTarget == inspector.RootTarget && e.OldValue != null && e.NewValue != null && inspector.checkBoxClearAfterDelve.IsChecked.HasValue && inspector.checkBoxClearAfterDelve.IsChecked.Value)
            {
                inspector.targetToFilter[e.OldValue.GetType()] = inspector.PropertiesFilter.Text;
                string text = string.Empty;
                inspector.targetToFilter.TryGetValue(e.NewValue.GetType(), out text);
                inspector.PropertiesFilter.Text = text ?? string.Empty;
            }
            inspector.lastRootTarget = inspector.RootTarget;
        }    

        private string GetDelvePath(Type rootTargetType)
        {
            StringBuilder delvePath = new StringBuilder(rootTargetType.Name);

            foreach (var propInfo in _delvePathList)
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

        private string GetCurrentTypeName(Type rootTargetType)
        {
            string type = string.Empty;
            if (_delvePathList.Count > 0)
            {
                ISkipDelve skipDelve = _delvePathList[_delvePathList.Count - 1].Value as ISkipDelve;
                if (skipDelve != null && skipDelve.NextValue != null && skipDelve.NextValueType != null)
                {
                    return skipDelve.NextValueType.ToString();//we want to make this "future friendly", so we take into account that the string value of the property type may change.
                }
                else if (_delvePathList[_delvePathList.Count - 1].Value != null)
                {
                    type = _delvePathList[_delvePathList.Count - 1].Value.GetType().ToString();
                }
                else
                {
                    type = _delvePathList[_delvePathList.Count - 1].PropertyType.ToString();
                }
            }
            else if (_delvePathList.Count == 0)
            {
                type = rootTargetType.FullName;
            }

            return type;
        }

        /// <summary>
        /// Delve Path
        /// </summary>
        public string DelvePath
        {
            get
            {
                if (this.RootTarget == null)
                    return "object is NULL";

                Type rootTargetType = this.RootTarget.GetType();
                string delvePath = GetDelvePath(rootTargetType);
                string type = GetCurrentTypeName(rootTargetType);

                return string.Format("{0}\n({1})", delvePath, type);
            }
        }

        public Type Type
        {
            get
            {
                if (this.Target != null)
                    return this.Target.GetType();
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
            PopTarget();
        }

        private void PopTarget()
        {
            if (this.inspectStack.Count > 1)
            {
                this.Target = this.inspectStack[this.inspectStack.Count - 2];
                this.inspectStack.RemoveAt(this.inspectStack.Count - 2);
                this.inspectStack.RemoveAt(this.inspectStack.Count - 2);

                if (this._delvePathList.Count > 0)
                {
                    this._delvePathList.RemoveAt(this._delvePathList.Count - 1);
                    this.OnPropertyChanged("DelvePath");
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
            ISkipDelve skipDelve = target as ISkipDelve;
            if (skipDelve != null)
            {
                return skipDelve.NextValue;
            }
            return target;
        }

        private void HandleDelve(object sender, ExecutedRoutedEventArgs e)
        {
            var realTarget = GetRealTarget(((PropertyInformation)e.Parameter).Value);

            if (realTarget != this.Target)
            {
                // top 'if' statement is the delve path.
                // we do this because without doing this, the delve path gets out of sync with the actual delves.
                // the reason for this is because PushTarget sets the new target,
                // and if it's equal to the current (original) target, we won't raise the property-changed event,
                // and therefore, we don't add to our delveStack (the real one).

                this._delvePathList.Add(((PropertyInformation)e.Parameter));
                this.OnPropertyChanged("DelvePath");
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
                e.CanExecute = true;
            e.Handled = true;
        }
        private void CanDelveBinding(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).Binding != null)
                e.CanExecute = true;
            e.Handled = true;
        }
        private void CanDelveBindingExpression(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).BindingExpression != null)
                e.CanExecute = true;
            e.Handled = true;
        }

        public PropertyFilter PropertyFilter
        {
            get { return propertyFilter; }
        }
        private PropertyFilter propertyFilter = new PropertyFilter(string.Empty, true);

        public string StringFilter
        {
            get { return this.propertyFilter.FilterString; }
            set
            {
                this.propertyFilter.FilterString = value;

                this.inspector.Filter = this.propertyFilter;

                this.OnPropertyChanged("StringFilter");
            }
        }

        public bool ShowDefaults
        {
            get { return this.propertyFilter.ShowDefaults; }
            set
            {
                this.propertyFilter.ShowDefaults = value;

                this.inspector.Filter = this.propertyFilter;

                this.OnPropertyChanged("ShowDefaults");
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
                PopTarget();
            }
        }
        private void PropertyInspector_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Left)
            {
                PopTarget();
            }
        }

        /// <summary>
        /// Hold the SelectedFilterSet in the PropertyFilter class, but track it here, so we know
        /// when to "refresh" the filtering with filterCall.Enqueue
        /// </summary>
        public PropertyFilterSet SelectedFilterSet
        {
            get { return propertyFilter.SelectedFilterSet; }
            set
            {
                propertyFilter.SelectedFilterSet = value;
                OnPropertyChanged("SelectedFilterSet");

                if (value == null)
                    return;

                if (value.IsEditCommand)
                {
                    var dlg = new EditUserFilters { UserFilters = CopyFilterSets(UserFilterSets) };

                    // set owning window to center over if we can find it up the tree
                    var snoopWindow = VisualTreeHelper2.GetAncestor<Window>(this);
                    if (snoopWindow != null)
                    {
                        dlg.Owner = snoopWindow;
                        dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }

                    bool? res = dlg.ShowDialog();
                    if (res.GetValueOrDefault())
                    {
                        // take the adjusted values from the dialog, setter will SAVE them to user properties
                        UserFilterSets = CleansFilterPropertyNames(dlg.ItemsSource);
                        // trigger the UI to re-bind to the collection, so user sees changes they just made
                        OnPropertyChanged("AllFilterSets");
                    }

                    // now that we're out of the dialog, set current selection back to "(default)"
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate
                    {
                        // couldnt get it working by setting SelectedFilterSet directly
                        // using the Index to get us back to the first item in the list
                        FilterSetCombo.SelectedIndex = 0;
                        //SelectedFilterSet = AllFilterSets[0];
                        return null;
                    }, null);
                }
                else
                {
                    this.inspector.Filter = this.propertyFilter;
                    OnPropertyChanged("SelectedFilterSet");
                }
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
                if (_filterSets == null)
                {
                    var ret = new List<PropertyFilterSet>();

                    try
                    {
                        var userFilters = Properties.Settings.Default.PropertyFilterSets;
                        ret.AddRange(userFilters ?? _defaultFilterSets);
                    }
                    catch (Exception exception)
                    {
                        ErrorDialog.ShowDialog(exception, "Error reading user filters from settings. Using default filters.", exceptionAlreadyHandled: true);
                        ret.Clear();
                        ret.AddRange(_defaultFilterSets);
                    }

                    _filterSets = ret.ToArray();
                }
                return _filterSets;
            }
            set
            {
                _filterSets = value;
                Properties.Settings.Default.PropertyFilterSets = _filterSets;
                Properties.Settings.Default.Save();
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
                var ret = new List<PropertyFilterSet>(UserFilterSets);

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
                return ret.ToArray();
            }
        }

        /// <summary>
        /// Make a deep copy of the filter collection.
        /// This is used when heading into the Edit dialog, so the user is editing a copy of the
        /// filters, in case they cancel the dialog - we dont want to alter their live collection.
        /// </summary>
        public PropertyFilterSet[] CopyFilterSets(PropertyFilterSet[] source)
        {
            var ret = new List<PropertyFilterSet>();
            foreach (PropertyFilterSet src in source)
            {
                ret.Add
                (
                    new PropertyFilterSet
                    {
                        DisplayName = src.DisplayName,
                        IsDefault = src.IsDefault,
                        IsEditCommand = src.IsEditCommand,
                        Properties = (string[])src.Properties.Clone()
                    }
                );
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Cleanse the property names in each filter in the collection.
        /// This includes removing spaces from each one, and making them all lower case
        /// </summary>
        private PropertyFilterSet[] CleansFilterPropertyNames(IEnumerable<PropertyFilterSet> collection)
        {
            foreach (PropertyFilterSet filterItem in collection)
            {
                filterItem.Properties = filterItem.Properties.Select(s => s.ToLower().Trim()).ToArray();
            }
            return collection.ToArray();
        }

        private List<object> inspectStack = new List<object>();
        private PropertyFilterSet[] _filterSets;
        private List<PropertyInformation> _delvePathList = new List<PropertyInformation>();

        private Inspector inspector;
        private object lastRootTarget = null;
        private readonly Dictionary<object, string> targetToFilter = new Dictionary<object, string>();

        private readonly PropertyFilterSet[] _defaultFilterSets = new PropertyFilterSet[]
		{
			new PropertyFilterSet
			{
				DisplayName = "Layout",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"width", "height", "actualwidth", "actualheight", "margin", "padding", "left", "top"
				}
			},
			new PropertyFilterSet
			{
				DisplayName = "Grid/Dock",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"grid", "dock"
				}
			},
			new PropertyFilterSet
			{
				DisplayName = "Color",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"color", "background", "foreground", "borderbrush", "fill", "stroke"
				}
			},
			new PropertyFilterSet
			{
				DisplayName = "ItemsControl",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"items", "selected"
				}
			}
		};

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
