#if NET5_0_OR_GREATER
namespace Snoop.Infrastructure
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;

    public static class NativeExports
    {
        [UnmanagedCallersOnly]
        public static int StartSnoop(IntPtr classNamePtr, IntPtr methodNamePtr, IntPtr settingsFilePtr)
        {
            //var assembly = Assembly.GetExecutingAssembly();

            //var assembliesBefore = AssemblyLoadContext.Default.Assemblies.ToArray();
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);
            //var assembliesAfter = AssemblyLoadContext.Default.Assemblies.ToArray();

            var className = Marshal.PtrToStringAuto(classNamePtr)!;
            var methodName = Marshal.PtrToStringAuto(methodNamePtr)!;
            var settingsFile = Marshal.PtrToStringAuto(settingsFilePtr)!;

            var result = (int)assembly
                .GetType(className)!
                .GetMethod(methodName)!
                .Invoke(null, new object[] { settingsFile })!;

            return result;
        }
    }
}
#endif