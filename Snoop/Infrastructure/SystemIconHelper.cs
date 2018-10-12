namespace Snoop.Infrastructure
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public enum SystemIcon
    {
        UACShield = 106
    }

    public static class SystemIconHelper
    {
        public static ImageSource GetImageSource(SystemIcon systemIcon, int width, int height)
        {
            switch (systemIcon)
            {
                case SystemIcon.UACShield:
                    return GetImageSource("#106", width, height);
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(systemIcon), systemIcon, null);
            }
        }

        private static ImageSource GetImageSource(string name, int width, int height)
        {
            const int LR_SHARED = 0x00008000;
            const int IMAGE_ICON = 1;
	        
            var image = NativeMethods.LoadImage(IntPtr.Zero, name, IMAGE_ICON, width, height, LR_SHARED);
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(image, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }
    }
}