// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;

namespace Snoop
{
    using System.Linq;
    using Snoop.Infrastructure;

    public class ApplicationTreeItem : ResourceContainerItem
	{
	    private readonly Application application;

		public ApplicationTreeItem(Application application, VisualTreeItem parent)
			: base(application, parent)
		{
			this.application = application;
		    this.IsExpanded = true;
		}

		public override Visual MainVisual => this.application.MainWindow;

	    protected override ResourceDictionary ResourceDictionary => this.application.Resources;

	    protected override void Reload(List<VisualTreeItem> toBeRemoved)
		{
			// having the call to base.Reload here ... puts the application resources at the very top of the tree view
			base.Reload(toBeRemoved);

		    foreach (Window window in this.application.Windows)
		    { 
		        if (window.IsInitialized == false
		            || window.CheckAccess() == false
		            || window.IsPartOfSnoopVisualTree())
		        { 
		            continue; 
		        }

                // windows which have an owner are added as child items in VisualItem, so we have to skip them here
		        if (window.Owner != null)
		        {
                    continue;
		        }

                // don't recreate existing items but reload them instead
		        var existingItem = toBeRemoved.FirstOrDefault(x => ReferenceEquals(x.Target, window));
                if (existingItem != null)
		        {
		            toBeRemoved.Remove(existingItem);
		            existingItem.Reload();
                    continue;
		        }

		        this.Children.Add(VisualTreeItem.Construct(window, this)); 
		    }
		}		
	}
}