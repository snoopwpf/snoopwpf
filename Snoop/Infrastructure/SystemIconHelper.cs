namespace Snoop.Infrastructure
{
    using System;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public static class SystemIconHelper
    {
        public enum SystemIcon
        {
            UACShield = 106
        }

        public static ImageSource GetImageSource(SystemIcon systemIcon)
        {
            switch (systemIcon)
            {
                case SystemIcon.UACShield:
                    return GetImageSource("#106");
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(systemIcon), systemIcon, null);
            }
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