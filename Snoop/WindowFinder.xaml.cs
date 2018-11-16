// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Reflection;

namespace Snoop
{
	public enum WindowFinderType { Snoop, Magnify };

	public partial class WindowFinder
	{
	    private WindowInfo _windowUnderCursor;
	    private SnoopabilityFeedbackWindow _feedbackWindow;
	    private IntPtr _feedbackWindowHandle;
	    private Cursor _crosshairsCursor;

	    public WindowFinder()
		{
		    this.InitializeComponent();

		    this._crosshairsCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Snoop.Resources.SnoopCrosshairsCursor.cur"));
		}

	    private bool IsDragging { get; set; }

		public WindowFinderType WindowFinderType { get; set; }

	    /// <inheritdoc />
	    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
	    {
	        this.StartSnoopTargetsSearch();
	        e.Handled = true;

            base.OnPreviewMouseLeftButtonDown(e);
	    }

	    /// <inheritdoc />
	    protected override void OnMouseMove(MouseEventArgs e)
	    {
            base.OnMouseMove(e);

	        if (!this.IsDragging)
	        {
	            return;
	        }

	        if (Mouse.LeftButton == MouseButtonState.Released)
	        {
	            this.StopSnoopTargetsSearch();
	            return;
	        }
			
	        var windowUnderCursor = NativeMethods.GetWindowUnderMouse();
	        if (this._windowUnderCursor == null)
	        {
	            this._windowUnderCursor = new WindowInfo(windowUnderCursor);
	        }

	        if (this.IsVisualFeedbackWindow(windowUnderCursor))
	        {
	            // if the window under the cursor is the feedback window, just ignore it.
	            return;
	        }

	        if (windowUnderCursor != this._windowUnderCursor.HWnd)
	        {
	            // the window under the cursor has changed
	            this.RemoveVisualFeedback();
	            this._windowUnderCursor = new WindowInfo(windowUnderCursor);

	            if (this._windowUnderCursor.IsValidProcess)
	            {
	                this.ShowVisualFeedback();
	            }
	        }

	        this.UpdateFeedbackWindowPosition();
	    }

	    /// <inheritdoc />
	    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
	    {
            base.OnMouseLeftButtonUp(e);

		    this.StopSnoopTargetsSearch();

			if (this._windowUnderCursor != null 
			    && this._windowUnderCursor.IsValidProcess)
			{
				if (this.WindowFinderType == WindowFinderType.Snoop)
				{
				    this.AttachSnoop();
				}
				else if (this.WindowFinderType == WindowFinderType.Magnify)
				{
				    this.AttachMagnify();
				}
			}
		}

		private void StartSnoopTargetsSearch()
		{
		    this.CaptureMouse();
		    this.IsDragging = true;
		    this.Cursor = this._crosshairsCursor;
		    this.snoopCrosshairsImage.Visibility = Visibility.Hidden;
		    this._windowUnderCursor = null;
		}

		private void StopSnoopTargetsSearch()
		{
		    this.ReleaseMouseCapture();
		    this.IsDragging = false;
		    this.Cursor = Cursors.Arrow;
		    this.snoopCrosshairsImage.Visibility = Visibility.Visible;
		    this.RemoveVisualFeedback();

		    // clear out cached process info to make the force refresh do the process check over again.
		    WindowInfo.ClearCachedWindowHandleInfo();
		}

		private void ShowVisualFeedback()
		{
			if (this._feedbackWindow == null)
			{
			    this._feedbackWindow = new SnoopabilityFeedbackWindow();
                this._feedbackWindow.PreviewKeyDown += this._feedbackWindow_PreviewKeyDown;
				// we don't have to worry about not having an application or not having a main window,
				// for, we are still in Snoop's process and not in the injected process.
				// so, go ahead and grab the Application.Current.MainWindow.
			    this._feedbackWindow.Owner = Application.Current.MainWindow;
			}

			if (!this._feedbackWindow.IsVisible)
			{
			    this._feedbackWindow.DataContext = this._windowUnderCursor;

			    this.UpdateFeedbackWindowPosition();
			    this._feedbackWindow.Show();

				if (this._feedbackWindowHandle == IntPtr.Zero)
				{
					var wih = new WindowInteropHelper(this._feedbackWindow);
				    this._feedbackWindowHandle = wih.Handle;
				}
			}
		}

        private void _feedbackWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.StopSnoopTargetsSearch();
            }
        }

        private void RemoveVisualFeedback()
		{
			if (this._feedbackWindow != null 
			    && this._feedbackWindow.IsVisible)
			{
			    this._feedbackWindow.Hide();
			}
		}

		private bool IsVisualFeedbackWindow(IntPtr hwnd)
		{
			return hwnd != IntPtr.Zero 
			       && hwnd == this._feedbackWindowHandle;
		}

		private void UpdateFeedbackWindowPosition()
		{
			if (this._feedbackWindow != null)
			{
				var mouse = NativeMethods.GetCursorPosition();
			    this._feedbackWindow.Left = mouse.X - 34;//.Left;
			    this._feedbackWindow.Top = mouse.Y + 10; // windowRect.Top;
			}
		}

		private void AttachSnoop()
		{
			new AttachFailedHandler(this._windowUnderCursor);
		    this._windowUnderCursor.Snoop();
		}

		private void AttachMagnify()
		{
			new AttachFailedHandler(this._windowUnderCursor);
		    this._windowUnderCursor.Magnify();
		}
	}	
}