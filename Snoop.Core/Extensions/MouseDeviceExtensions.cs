// ReSharper disable once CheckNamespace
namespace Snoop
{
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;

    public static class MouseDeviceExtensions
    {
        private static readonly PropertyInfo rawDirectlyOverPropertyInfo = typeof(MouseDevice).GetProperty("RawDirectlyOver", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static IInputElement GetRawDirectlyOver(this MouseDevice mouseDevice)
        {
            if (rawDirectlyOverPropertyInfo == null)
            {
                return mouseDevice.DirectlyOver;
            }

            return rawDirectlyOverPropertyInfo.GetValue(mouseDevice, null) as IInputElement;
        }
    }
}