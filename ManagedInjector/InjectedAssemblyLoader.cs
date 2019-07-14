namespace ManagedInjector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Xml.Serialization;
    using JetBrains.Annotations;

    /// <summary>
    /// Class responsible for loading the main assembly and calling it's startup method after this class got injected into a foreign process.
    /// </summary>
    public static class InjectedAssemblyLoader
    {
        [PublicAPI]
        [DllExport]
        public static void LoadAssemblyAndCallStartupMethod([MarshalAs(UnmanagedType.LPWStr)] string transportDataString)
        {
            //Debugger.Launch();

            Trace.WriteLine($"Beginning load in foreign process...");
            Trace.WriteLine("Framework information:");
            Trace.WriteLine(new FrameworkAndSystemVersionInfo());

            InjectorData injectorData;

            {
                var serializer = new XmlSerializer(typeof(InjectorData));

                using (var stream = new StringReader(transportDataString))
                {
                    injectorData = (InjectorData)serializer.Deserialize(stream);
                }
            }

            Trace.WriteLine($"Loading assembly '{injectorData.FullAssemblyPath}'...");

            var assembly = Assembly.LoadFrom(injectorData.FullAssemblyPath);

            Trace.WriteLine($"Assembly '{injectorData.FullAssemblyPath}' loaded.");

            var type = assembly.GetType(injectorData.ClassName);

            if (type == null)
            {
                Trace.WriteLine($"{injectorData.ClassName} could not be found in {injectorData.FullAssemblyPath}");
                return;
            }

            var method = type.GetMethod(injectorData.MethodName, BindingFlags.Static | BindingFlags.Public);

            if (method != null)
            {
                Trace.WriteLine($"Invoking startup method...");

                method.Invoke(null, new[]
                                    {
                                        injectorData.SettingsFile
                                    });
            }
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