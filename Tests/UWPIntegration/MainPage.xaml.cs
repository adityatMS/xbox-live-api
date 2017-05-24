// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 

namespace UWPIntegration
{
    using System;
    using System.Linq;

    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Windows.UI.Xaml.Data;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
    }

    

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private static IntPtr _xsapiNativeDll;

        public MainPage()
        {
            this.InitializeComponent();
            _xsapiNativeDll = LoadNativeDll(@"Microsoft.Xbox.Services.141.UWP.C.dll");
            DoWork();
        }

        public static IntPtr LoadNativeDll(string fileName)
        {
            IntPtr nativeDll = NativeMethods.LoadLibrary(fileName);
            if (nativeDll == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return nativeDll;
        }

        public static T Invoke<T, T2>(IntPtr library, params object[] args)
        {
            IntPtr procAddress = NativeMethods.GetProcAddress(library, typeof(T2).Name);
            if (procAddress == IntPtr.Zero)
            {
                return default(T);
            }

            var function = Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T2));
            return (T)function.DynamicInvoke(args);
        }

        public static void Invoke<T>(IntPtr library, params object[] args)
        {
            IntPtr procAddress = NativeMethods.GetProcAddress(library, typeof(T).Name);
            if (procAddress == IntPtr.Zero)
            {
                return;
            }

            var function = Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
            function.DynamicInvoke(args);
        }

        private delegate double xbl_get_version();

        private void SignInSilentButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private bool doOnce = true;
        async void DoWork()
        {
            while (doOnce)
            {
                doOnce = false;

                Debug.WriteLine("XBL Version: " + Invoke<double, xbl_get_version>(_xsapiNativeDll));

                // don't run again for at least 200 milliseconds
                await Task.Delay(200);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}