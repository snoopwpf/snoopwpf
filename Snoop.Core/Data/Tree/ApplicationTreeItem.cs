// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree
{
    using System.Windows;
    using System.Windows.Media;
    using Snoop.Infrastructure;

    public class ApplicationTreeItem : ResourceContainerTreeItem
    {
        private readonly Application application;

        public ApplicationTreeItem(Application application, TreeItem parent, TreeService treeService)
            : base(application, parent, treeService)
        {
            this.application = application;
            this.IsExpanded = true;
        }

        public override Visual MainVisual => this.application.MainWindow;

        protected override ResourceDictionary ResourceDictionary => this.application.Resources;

        protected override void ReloadCore()
        {
            // having the call to base.ReloadCore here ... puts the application resources at the very top of the tree view
            base.ReloadCore();

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

                this.Children.Add(this.TreeService.Construct(window, this));
            }
        }
    }
}