namespace ManagedInjector
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Xml.Serialization;
    using JetBrains.Annotations;
    using Snoop;

    public static class MessageHookClass
    {
        private static readonly uint messageId;

        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        private struct CWPSTRUCT
        {
            public IntPtr lparam;
            public IntPtr wparam;
            public int message;
            public IntPtr hwnd;
        }

        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        static MessageHookClass()
        {
            messageId = NativeMethods.RegisterWindowMessage("Injector_GOBABYGO!");
        }

        [PublicAPI]
        [DllExport]
        public static int MessageHookProc(int code, IntPtr wparam, IntPtr lparam)
        {
            if (code == 0)
            {
                var msg = (CWPSTRUCT)Marshal.PtrToStructure(lparam, typeof(CWPSTRUCT));

                if (msg.message == messageId)
                {
                    //Debugger.Launch();

                    Trace.WriteLine($"MessageHookProc in .NET {code} {msg.wparam} {msg.lparam}");
                    Trace.WriteLine(new FrameworkAndSystemVersionInfo());

                    var transportDataString = Marshal.PtrToStringUni(msg.wparam);
                    InjectorData injectorData;

                    {
                        var serializer = new XmlSerializer(typeof(InjectorData));

                        using (var stream = new StringReader(transportDataString))
                        {
                            injectorData = (InjectorData)serializer.Deserialize(stream);
                        }
                    }

                    var assembly = Assembly.LoadFrom(injectorData.AssemblyName);

                    var type = assembly.GetType(injectorData.ClassName);

                    if (type != null)
                    {
                        var method = type.GetMethod(injectorData.MethodName, BindingFlags.Static | BindingFlags.Public);

                        if (method != null)
                        {
                            method.Invoke(null, new[]
                                                {
                                                    injectorData.SettingsFile
                                                });
                        }
                    }
                }
            }

            return CallNextHookEx(IntPtr.Zero, code, wparam, lparam);
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DllExportAttribute : Attribute
    {
        [PublicAPI]
        public string EntryPoint { get; set; }

        [PublicAPI]
        public CallingConvention CallingConvention { get; set; } = CallingConvention.Cdecl;
    }

    public class FrameworkAndSystemVersionInfo
    {
        /// <inheritdoc />
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(".NET version:");
            var targetFrameworkAttribute = Assembly.GetExecutingAssembly().GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().SingleOrDefault();
            stringBuilder.AppendLine($"TargetFrameworkAttribute.FrameworkName: {targetFrameworkAttribute?.FrameworkName}");
            stringBuilder.AppendLine($"TargetFrameworkAttribute.FrameworkDisplayName: {targetFrameworkAttribute?.FrameworkDisplayName}");
            stringBuilder.AppendLine($"Environment.Version: {Environment.Version}");
#if NETCOREAPP
            stringBuilder.AppendLine($"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}");

            stringBuilder.AppendLine($"CoreCLR Build: {((AssemblyInformationalVersionAttribute[])typeof(object).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))[0].InformationalVersion.Split('+')[0]}");
            stringBuilder.AppendLine($"CoreCLR Hash: {((AssemblyInformationalVersionAttribute[])typeof(object).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))[0].InformationalVersion.Split('+')[1]}");
            stringBuilder.AppendLine($"CoreFX Build: {((AssemblyInformationalVersionAttribute[])typeof(Uri).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))[0].InformationalVersion.Split('+')[0]}");
            stringBuilder.AppendLine($"CoreFX Hash: {((AssemblyInformationalVersionAttribute[])typeof(Uri).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))[0].InformationalVersion.Split('+')[1]}");
#endif
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("OS Version");
            stringBuilder.AppendLine($"Environment.OSVersion: {Environment.OSVersion}");
#if NETCOREAPP
            stringBuilder.AppendLine($"RuntimeInformation.OSDescription: {RuntimeInformation.OSDescription}");
#endif
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("Bitness");
#if NETCOREAPP
            stringBuilder.AppendLine($"RuntimeInformation.OSArchitecture: {RuntimeInformation.OSArchitecture}");
            stringBuilder.AppendLine($"RuntimeInformation.ProcessArchitecture: {RuntimeInformation.ProcessArchitecture}");
#endif
            stringBuilder.AppendLine($"Environment.Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
            stringBuilder.AppendLine($"Environment.Is64BitProcess: {Environment.Is64BitProcess}");

            return stringBuilder.ToString();
        }
    }
}