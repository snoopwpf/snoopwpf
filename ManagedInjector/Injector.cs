using System;

namespace ManagedInjector2
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net.Mime;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Xml.Serialization;
    //using Snoop;

    public static class Injector
    {
        private static readonly uint messageId;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

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

        static Injector()
        {
            messageId = RegisterWindowMessage("Injector_GOBABYGO!");
        }

        [DllExport]
        public static int MessageHookProc(int code, IntPtr wparam, IntPtr lparam)
        {
            if (code == 0)
            {
                var msg = (CWPSTRUCT)Marshal.PtrToStructure(lparam, typeof(CWPSTRUCT));

                if (msg.message == messageId)
                {
                    Trace.WriteLine($"MessageHookProc in .NET {code} {wparam} {lparam}");

                    MessageBox.Show("Hi from snoop");

                    //var transportDataString = Marshal.PtrToStringUni(wparam);
                    //InjectorData injectorData = null;

                    //{
                    //    var serializer = new XmlSerializer(typeof(InjectorData));

                    //    using (var stream = new StringReader(transportDataString))
                    //    {
                    //        injectorData = (InjectorData)serializer.Deserialize(stream);
                    //    }
                    //}

                    //var assembly = Assembly.Load(injectorData.AssemblyName);

                    //if (assembly != null)
                    //{
                    //    var type = assembly.GetType(injectorData.ClassName);

                    //    if (type != null)
                    //    {
                    //        var method = type.GetMethod(injectorData.MethodName, BindingFlags.Static | BindingFlags.Public);

                    //        if (method != null)
                    //        {
                    //            method.Invoke(null, new[]
                    //                                {
                    //                                    injectorData.SettingsFile
                    //                                });
                    //        }
                    //    }
                    //}
                }
            }

            return CallNextHookEx(IntPtr.Zero, code, wparam, lparam);
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DllExportAttribute : Attribute
    {
        //public DllExportAttribute(string exportName, CallingConvention callingConvention) 
        //{
        //    this.ExportName = exportName;
        //    this.CallingConvention = callingConvention;
        //}

        //public DllExportAttribute(string exportName) 
        //{ 
        //    this.ExportName = exportName;
        //}

        //public DllExportAttribute(CallingConvention callingConvention) 
        //{
        //    this.CallingConvention = callingConvention;
        //}

        //public DllExportAttribute()
        //{
        //}

#pragma warning disable IDE0052 // Remove unread private members
        // ReSharper disable once UnusedMember.Local
        private static readonly object forceReference = typeof(CallConvCdecl);
#pragma warning restore IDE0052 // Remove unread private members

        public string EntryPoint { get; set; }

        public CallingConvention CallingConvention { get; set; } = CallingConvention.Cdecl;
    }
}