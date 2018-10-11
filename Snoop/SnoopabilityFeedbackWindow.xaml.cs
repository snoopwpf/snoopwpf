// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public partial class SnoopabilityFeedbackWindow
	{
		public SnoopabilityFeedbackWindow()
		{
		    this.InitializeComponent();

            this.SetCurrentValue(UACImageSourceProperty, GetImageSource("#106"));
		}

	    public static readonly DependencyProperty UACImageSourceProperty = DependencyProperty.Register(nameof(UACImageSource), typeof(ImageSource), typeof(SnoopabilityFeedbackWindow), new PropertyMetadata(default(ImageSource)));

	    public ImageSource UACImageSource
	    {
	        get { return (ImageSource)this.GetValue(UACImageSourceProperty); }
	        set { this.SetValue(UACImageSourceProperty, value); }
	    }

	    private static ImageSource GetImageSource(string name)
	    {
	        const int LR_SHARED = 0x00008000;
            const int IMAGE_ICON = 1;
	        
	        var image = NativeMethods.LoadImage(IntPtr.Zero, name, IMAGE_ICON, SystemInformation.IconSize.Width, SystemInformation.IconSize.Height, LR_SHARED);
	        var imageSource = Imaging.CreateBitmapSourceFromHIcon(image, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

	        return imageSource;
	    }
	}
}