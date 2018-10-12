// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System.Windows;
    using System.Windows.Media;
    using Snoop.Infrastructure;

    public partial class SnoopabilityFeedbackWindow
	{
		public SnoopabilityFeedbackWindow()
		{
		    this.InitializeComponent();

            this.SetCurrentValue(UACImageSourceProperty, SystemIconHelper.GetImageSource(SystemIconHelper.SystemIcon.UACShield));
		}

	    public static readonly DependencyProperty UACImageSourceProperty = DependencyProperty.Register(nameof(UACImageSource), typeof(ImageSource), typeof(SnoopabilityFeedbackWindow), new PropertyMetadata(default(ImageSource)));

	    public ImageSource UACImageSource
	    {
	        get { return (ImageSource)this.GetValue(UACImageSourceProperty); }
	        set { this.SetValue(UACImageSourceProperty, value); }
	    }
	}
}