// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop
{
    using JetBrains.Annotations;

    public sealed partial class SnoopUI : INotifyPropertyChanged
	{
		#region Public Static Routed Commands
		public static readonly RoutedCommand IntrospectCommand = new RoutedCommand("Introspect", typeof(SnoopUI));
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand("Refresh", typeof(SnoopUI));
		public static readonly RoutedCommand HelpCommand = new RoutedCommand("Help", typeof(SnoopUI));
		public static readonly RoutedCommand InspectCommand = new RoutedCommand("Inspect", typeof(SnoopUI));
		public static readonly RoutedCommand SelectFocusCommand = new RoutedCommand("SelectFocus", typeof(SnoopUI));
		public static readonly RoutedCommand SelectFocusScopeCommand = new RoutedCommand("SelectFocusScope", typeof(SnoopUI));
		public static readonly RoutedCommand ClearSearchFilterCommand = new RoutedCommand("ClearSearchFilter", typeof(SnoopUI));
		public static readonly RoutedCommand CopyPropertyChangesCommand = new RoutedCommand("CopyPropertyChanges", typeof(SnoopUI));
		#endregion

		#region Static Constructor
		static SnoopUI()
		{
			IntrospectCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
			RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
			HelpCommand.InputGestures.Add(new KeyGesture(Key.F1));
			ClearSearchFilterCommand.InputGestures.Add(new KeyGesture(Key.Escape));
			CopyPropertyChangesCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
		}
		#endregion

		#region Public Constructor
		public SnoopUI()
		{
			this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

			this.InitializeComponent();

			// wrap the following PresentationTraceSources.Refresh() call in a try/catch
			// sometimes a NullReferenceException occurs
			// due to empty <filter> elements in the app.config file of the app you are snooping
			// see the following for more info:
			// http://snoopwpf.codeplex.com/discussions/236503
			// http://snoopwpf.codeplex.com/workitem/6647
			try
			{
				PresentationTraceSources.Refresh();
				PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
			}
			catch (NullReferenceException)
			{
				// swallow this exception since you can Snoop just fine anyways.
			}

			this.CommandBindings.Add(new CommandBinding(IntrospectCommand, this.HandleIntrospection));
			this.CommandBindings.Add(new CommandBinding(RefreshCommand, this.HandleRefresh));
			this.CommandBindings.Add(new CommandBinding(HelpCommand, this.HandleHelp));

			this.CommandBindings.Add(new CommandBinding(InspectCommand, this.HandleInspect));

			this.CommandBindings.Add(new CommandBinding(SelectFocusCommand, this.HandleSelectFocus));
			this.CommandBindings.Add(new CommandBinding(SelectFocusScopeCommand, this.HandleSelectFocusScope));

			//NOTE: this is up here in the outer UI layer so ESC will clear any typed filter regardless of where the focus is
			// (i.e. focus on a selected item in the tree, not in the property list where the search box is hosted)
			this.CommandBindings.Add(new CommandBinding(ClearSearchFilterCommand, this.ClearSearchFilterHandler));

			this.CommandBindings.Add(new CommandBinding(CopyPropertyChangesCommand, this.CopyPropertyChangesHandler));

			InputManager.Current.PreProcessInput += this.HandlePreProcessInput;
			this.Tree.SelectedItemChanged += this.HandleTreeSelectedItemChanged;

			// we can't catch the mouse wheel at the ZoomerControl level,
			// so we catch it here, and relay it to the ZoomerControl.
			this.MouseWheel += this.SnoopUI_MouseWheel;

            this.filterTimer = new DispatcherTimer
                               {
                                   Interval = TimeSpan.FromSeconds(0.3)
                               };
            this.filterTimer.Tick += (s, e) =>
			{
                this.EnqueueAfterSettingFilter();
                this.filterTimer.Stop();
			};
        }

        #endregion

		#region Public Properties
		#region VisualTreeItems
		/// <summary>
		/// This is the collection of VisualTreeItem(s) that the visual tree TreeView binds to.
		/// </summary>
		public ObservableCollection<VisualTreeItem> VisualTreeItems
		{
			get { return this.visualTreeItems; }
		}
		#endregion

		#region Root

	    /// <summary>
	    /// Root element of the visual tree
	    /// </summary>
	    public VisualTreeItem Root
	    {
	        get { return this.rootVisualTreeItem; }
            private set
            {
                this.rootVisualTreeItem = value;
                this.OnPropertyChanged(nameof(this.Root));
            }
	    }

	    /// <summary>
		/// rootVisualTreeItem is the VisualTreeItem for the root you are inspecting.
		/// </summary>
		private VisualTreeItem rootVisualTreeItem;
		/// <summary>
		/// root is the object you are inspecting.
		/// </summary>
		private object root;
		#endregion

		#region CurrentSelection

        public override object Target
        {
            get => this.currentSelection?.Target;
            set => this.currentSelection = this.FindItem(value);
        }

        /// <summary>
        /// Currently selected item in the tree view.
        /// </summary>
        public VisualTreeItem CurrentSelection
		{
			get { return this.currentSelection; }
			set
			{
				if (this.currentSelection != value)
				{
					if (this.currentSelection != null)
					{
                        this.SaveEditedProperties(this.currentSelection);
						this.currentSelection.IsSelected = false;
					}

					this.currentSelection = value;

					if (this.currentSelection != null)
					{
						this.currentSelection.IsSelected = true;
                        this._lastNonNullSelection = this.currentSelection;
					}

					this.OnPropertyChanged(nameof(this.CurrentSelection));
					this.OnPropertyChanged(nameof(this.CurrentFocusScope));

					if (this.visualTreeItems.Count > 1 || this.visualTreeItems.Count == 1 && this.visualTreeItems[0] != this.rootVisualTreeItem)
					{
						// Check whether the selected item is filtered out by the filter,
						// in which case reset the filter.
						var tmp = this.currentSelection;
						while (tmp != null && !this.visualTreeItems.Contains(tmp))
						{
							tmp = tmp.Parent;
						}
						if (tmp == null)
						{
							// The selected item is not a descendant of any root.
							RefreshCommand.Execute(null, this);
						}
					}
				}
			}
		}

		private VisualTreeItem currentSelection = null;
		private VisualTreeItem _lastNonNullSelection = null;

		#endregion

		#region Filter
		/// <summary>
		/// This Filter property is bound to the editable combo box that the user can type in to filter the visual tree TreeView.
		/// Every time the user types a key, the setter gets called, enqueueing a delayed call to the ProcessFilter method.
		/// </summary>
		public string Filter
		{
			get { return this.filter; }
			set
			{
				this.filter = value;

				if (!this.fromTextBox)
				{
                    this.EnqueueAfterSettingFilter();
				}
				else
				{
                    this.filterTimer.Stop();
                    this.filterTimer.Start();
				}
			}
		}

		private void SetFilter(string value)
		{
            this.fromTextBox = false;
			this.Filter = value;
            this.fromTextBox = true;
		}

		private void EnqueueAfterSettingFilter()
		{
			this.filterCall.Enqueue();

			this.OnPropertyChanged(nameof(this.Filter));
		}

		private string filter = string.Empty;
		#endregion

		#region EventFilter
		public string EventFilter
		{
			get { return this.eventFilter; }
			set
			{
				this.eventFilter = value;
				EventsListener.Filter = value;
			}
		}
		#endregion

		#region CurrentFocus
		public IInputElement CurrentFocus
		{
			get
			{
				var newFocus = Keyboard.FocusedElement;
				if (newFocus != this.currentFocus)
				{
					// Store reference to previously focused element only if focused element was changed.
					this.previousFocus = this.currentFocus;
				}
				this.currentFocus = newFocus;

				return this.returnPreviousFocus ? this.previousFocus : this.currentFocus;
			}
		}
		#endregion

		#region CurrentFocusScope
		public object CurrentFocusScope
		{
			get
			{
				if (this.CurrentSelection == null)
                {
                    return null;
                }

                var selectedItem = this.CurrentSelection.Target as DependencyObject;
				if (selectedItem != null)
				{
					return FocusManager.GetFocusScope(selectedItem);
				}
				return null;
			}
		}
		#endregion
		#endregion

		#region Public Methods

        public void ApplyReduceDepthFilter(VisualTreeItem newRoot)
		{
			if (this.m_reducedDepthRoot != newRoot)
			{
				if (this.m_reducedDepthRoot == null)
				{
                    this.Dispatcher.BeginInvoke
					(
						DispatcherPriority.Background,
						(function)
						delegate
						{
							this.visualTreeItems.Clear();
							this.visualTreeItems.Add(this.m_reducedDepthRoot);
                            this.m_reducedDepthRoot = null;
						}
					);
				}

                this.m_reducedDepthRoot = newRoot;
			}
		}

		/// <summary>
		/// Loop through the properties in the current PropertyGrid and save away any properties
		/// that have been changed by the user.  
		/// </summary>
		/// <param name="owningObject">currently selected object that owns the properties in the grid (before changing selection to the new object)</param>
		private void SaveEditedProperties( VisualTreeItem owningObject )
		{
            foreach (var property in this.PropertyGrid.PropertyGrid.Properties)
            {
                if (property.IsValueChangedByUser)
                {
                    EditedPropertiesHelper.AddEditedProperty(this.Dispatcher, owningObject, property );
                }
            }
		}

		#endregion

		#region Protected Event Overrides
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

		    // load whether all properties are shown by default
		    this.PropertyGrid.ShowDefaults = Properties.Settings.Default.ShowDefaults;

		    // load whether the previewer is shown by default
		    this.PreviewArea.IsActive = Properties.Settings.Default.ShowPreviewer;

		    // load the window placement details from the user settings.
            SnoopWindowUtils.LoadWindowPlacement(this, Properties.Settings.Default.SnoopUIWindowPlacement);
		}

		/// <summary>
		/// Cleanup when closing the window.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			this.CurrentSelection = null;

			InputManager.Current.PreProcessInput -= this.HandlePreProcessInput;
			EventsListener.Stop();

		    // persist the window placement details to the user settings.
		    SnoopWindowUtils.SaveWindowPlacement(this, wp => Properties.Settings.Default.SnoopUIWindowPlacement = wp);

			// persist whether all properties are shown by default
			Properties.Settings.Default.ShowDefaults = this.PropertyGrid.ShowDefaults;

			// persist whether the previewer is shown by default
			Properties.Settings.Default.ShowPreviewer = this.PreviewArea?.IsActive == true;

			// actually do the persisting
			Properties.Settings.Default.Save();
        }

		#endregion

		#region Private Routed Event Handlers
		/// <summary>
		/// Just for fun, the ability to run Snoop on itself :)
		/// </summary>
		private void HandleIntrospection(object sender, ExecutedRoutedEventArgs e)
		{
			this.Load(this);
		}

		private void HandleRefresh(object sender, ExecutedRoutedEventArgs e)
		{
			var saveCursor = Mouse.OverrideCursor;
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				var currentTarget = this.CurrentSelection != null ? this.CurrentSelection.Target : null;

				this.visualTreeItems.Clear();

				this.Root = VisualTreeItem.Construct(this.root, null);

				if (currentTarget != null)
				{
					var visualItem = this.FindItem(currentTarget);
					if (visualItem != null)
                    {
                        this.CurrentSelection = visualItem;
                    }
                }

				this.SetFilter(this.filter);
			}
			finally
			{
				Mouse.OverrideCursor = saveCursor;
			}
		}
		private void HandleHelp(object sender, ExecutedRoutedEventArgs e)
		{
			//Help help = new Help();
			//help.Show();
		}
		private void HandleInspect(object sender, ExecutedRoutedEventArgs e)
		{
			var visual = e.Parameter as Visual;
			if (visual != null)
			{
				var node = this.FindItem(visual);
				if (node != null)
                {
                    this.CurrentSelection = node;
                }
            }
			else if (e.Parameter != null)
			{
				this.PropertyGrid.SetTarget(e.Parameter);
			}
		}
		private void HandleSelectFocus(object sender, ExecutedRoutedEventArgs e)
		{
			// We know we've stolen focus here. Let's use previously focused element.
			this.returnPreviousFocus = true;
            this.SelectItem(this.CurrentFocus as DependencyObject);
			this.returnPreviousFocus = false;
            this.OnPropertyChanged(nameof(this.CurrentFocus));
		}

		private void HandleSelectFocusScope(object sender, ExecutedRoutedEventArgs e)
		{
            this.SelectItem(e.Parameter as DependencyObject);
		}

		private void ClearSearchFilterHandler(object sender, ExecutedRoutedEventArgs e)
		{
            this.PropertyGrid.StringFilter = string.Empty;
		}

		private void CopyPropertyChangesHandler(object sender, ExecutedRoutedEventArgs e)
		{
			if (this.currentSelection != null)
            {
                this.SaveEditedProperties(this.currentSelection);
            }

            EditedPropertiesHelper.DumpObjectsWithEditedProperties();
		}

		private void SelectItem(DependencyObject item)
		{
			if (item != null)
			{
				var node = this.FindItem(item);
				if (node != null)
                {
                    this.CurrentSelection = node;
                }
            }
		}
		#endregion

		#region Private Event Handlers

		private void HandlePreProcessInput(object sender, PreProcessInputEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.CurrentFocus));

			var currentModifiers = InputManager.Current.PrimaryKeyboardDevice.Modifiers;
			if (!((currentModifiers & ModifierKeys.Control) != 0 && (currentModifiers & ModifierKeys.Shift) != 0))
            {
                return;
            }

            var directlyOver = Mouse.PrimaryDevice.GetDirectlyOver() as Visual;
			if (directlyOver == null 
                || directlyOver.IsDescendantOf(this)
                || directlyOver.IsPartOfSnoopVisualTree())
            {
                return;
            }

            var node = this.FindItem(directlyOver);
			if (node != null)
            {
                this.CurrentSelection = node;
            }
        }

		private void SnoopUI_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			this.PreviewArea.Zoomer.DoMouseWheel(sender, e);
		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Find the VisualTreeItem for the specified visual.
		/// If the item is not found and is not part of the Snoop UI,
		/// the tree will be adjusted to include the window the item is in.
		/// </summary>
		private VisualTreeItem FindItem(object target)
		{
		    if (this.rootVisualTreeItem == null)
		    {
		        return null;
		    }

		    var node = this.rootVisualTreeItem.FindNode(target);
			var rootVisual = this.rootVisualTreeItem.MainVisual;
			if (node == null)
			{
				var visual = target as Visual;
				if (visual != null && rootVisual != null)
				{
					// If target is a part of the SnoopUI, let's get out of here.
					if (visual.IsDescendantOf(this))
					{
						return null;
					}

					// If not in the root tree, make the root be the tree the visual is in.
					if (!visual.IsDescendantOf(rootVisual))
					{
						var presentationSource = PresentationSource.FromVisual(visual);
						if (presentationSource == null)
						{
							return null; // Something went wrong. At least we will not crash with null ref here.
						}

						this.Root = new VisualItem(presentationSource.RootVisual, null);
					}
				}

				this.rootVisualTreeItem.Reload();

				node = this.rootVisualTreeItem.FindNode(target);

				this.SetFilter(this.filter);
			}
			return node;
		}

        private void HandleTreeSelectedItemChanged(object sender, EventArgs e)
		{
			var item = this.Tree.SelectedItem as VisualTreeItem;
			if (item != null)
            {
                this.CurrentSelection = item;
            }
        }

		private void ProcessFilter()
		{
			if (SnoopModes.MultipleDispatcherMode && !this.Dispatcher.CheckAccess())
			{
				Action action = () => this.ProcessFilter();
				this.Dispatcher.BeginInvoke(action);
				return;
			}

			this.visualTreeItems.Clear();

			// cplotts todo: we've got to come up with a better way to do this.
			if (this.filter == "Clear any filter applied to the tree view")
			{
				this.SetFilter(string.Empty);
			}
			else if (this.filter == "Show only visuals with binding errors")
			{
				this.FilterBindings(this.rootVisualTreeItem);
			}
			else if (this.filter.Length == 0)
			{
				this.visualTreeItems.Add(this.rootVisualTreeItem);
			}
			else
			{
				this.FilterTree(this.rootVisualTreeItem, this.filter.ToLower());
			}
		}

		private void FilterTree(VisualTreeItem node, string filter)
		{
			foreach (var child in node.Children)
			{
				if (child.Filter(filter))
                {
                    this.visualTreeItems.Add(child);
                }
                else
                {
                    this.FilterTree(child, filter);
                }
            }
		}
		private void FilterBindings(VisualTreeItem node)
		{
			foreach (var child in node.Children)
			{
				if (child.HasBindingError)
                {
                    this.visualTreeItems.Add(child);
                }
                else
                {
                    this.FilterBindings(child);
                }
            }
		}

		protected override void Load(object newRoot)
		{
			this.root = newRoot;

			this.visualTreeItems.Clear();

			this.Root = VisualTreeItem.Construct(newRoot, null);
			this.CurrentSelection = this.rootVisualTreeItem;

			this.SetFilter(this.filter);

			this.OnPropertyChanged(nameof(this.Root));
		}
		#endregion

		#region Private Fields

		private bool fromTextBox = true;
		private DispatcherTimer filterTimer;

		private ObservableCollection<VisualTreeItem> visualTreeItems = new ObservableCollection<VisualTreeItem>();

		private string propertyFilter = string.Empty;
		private string eventFilter = string.Empty;

		private DelayedCall filterCall;

		private VisualTreeItem m_reducedDepthRoot;

		private IInputElement currentFocus;
		private IInputElement previousFocus;

		/// <summary>
		/// Indicates whether CurrentFocus should retur previously focused element.
		/// This fixes problem where Snoop steals the focus from snooped app.
		/// </summary>
		private bool returnPreviousFocus;

        #endregion

		#region Private Delegates
		private delegate void function();
		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged(string propertyName)
		{
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

		#endregion
	}

	#region NoFocusHyperlink
	public class NoFocusHyperlink : Hyperlink
	{
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			this.OnClick();
		}
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
	}
	#endregion

	public class PropertyValueInfo
	{
		public string PropertyName { get; set; }
		public object PropertyValue { get; set; }
	}

	public class EditedPropertiesHelper
	{
		private static object _lock = new object();

		private static readonly Dictionary<Dispatcher,Dictionary<VisualTreeItem, List<PropertyValueInfo>>> _itemsWithEditedProperties =
			new Dictionary<Dispatcher,Dictionary<VisualTreeItem, List<PropertyValueInfo>>>();

		public static void AddEditedProperty( Dispatcher dispatcher, VisualTreeItem propertyOwner, PropertyInformation propInfo)
		{
			lock ( _lock )
			{
				List<PropertyValueInfo> propInfoList = null;
				Dictionary<VisualTreeItem, List<PropertyValueInfo>> dispatcherList = null;

				// first get the dictionary we're using for the given dispatcher
				if ( !_itemsWithEditedProperties.TryGetValue( dispatcher, out dispatcherList ) )
				{
					dispatcherList = new Dictionary<VisualTreeItem, List<PropertyValueInfo>>();
					_itemsWithEditedProperties.Add( dispatcher, dispatcherList );
				}

				// now get the property info list for the owning object 
				if ( !dispatcherList.TryGetValue( propertyOwner, out propInfoList ) )
				{
					propInfoList = new List<PropertyValueInfo>();
					dispatcherList.Add( propertyOwner, propInfoList );
				}

				// if we already have a property of that name on this object, remove it
				var existingPropInfo = propInfoList.FirstOrDefault( l => l.PropertyName == propInfo.DisplayName );
				if ( existingPropInfo != null )
				{
					propInfoList.Remove( existingPropInfo );
				}

				// finally add the edited property info
				propInfoList.Add( new PropertyValueInfo
				                  {
				                  	PropertyName = propInfo.DisplayName,
				                  	PropertyValue = propInfo.Value,
				                  } );
			}
		}

		public static void DumpObjectsWithEditedProperties()
		{
			if (_itemsWithEditedProperties.Count == 0)
			{
				return;
			}

			var sb = new StringBuilder();
			sb.AppendFormat
			(
				"Snoop dump as of {0}{1}--- OBJECTS WITH EDITED PROPERTIES ---{1}",
				DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
				Environment.NewLine
			);

			var dispatcherCount = 1;

			foreach (var dispatcherKVP in _itemsWithEditedProperties)
			{
				if ( _itemsWithEditedProperties.Count > 1 )
				{
					sb.AppendFormat( "-- Dispatcher #{0} -- {1}", dispatcherCount++, Environment.NewLine );
				}

				foreach (var objectPropertiesKVP in dispatcherKVP.Value )
				{
					sb.AppendFormat("Object: {0}{1}", objectPropertiesKVP.Key, Environment.NewLine); 
					foreach (var propInfo in objectPropertiesKVP.Value)
					{
						sb.AppendFormat
						(
							"\tProperty: {0}, New Value: {1}{2}",
							propInfo.PropertyName,
							propInfo.PropertyValue,
							Environment.NewLine
						);
					}
				}

				if ( _itemsWithEditedProperties.Count > 1 )
				{
					sb.AppendLine();
				}
			}

			Debug.WriteLine(sb.ToString());
		    ClipboardHelper.SetText(sb.ToString());
		}		
	}
}