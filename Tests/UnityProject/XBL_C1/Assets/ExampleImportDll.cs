using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using System.Runtime.InteropServices;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ExampleImportDll : MonoBehaviour 
{
//#if UNITY_IPHONE
//       // On iOS plugins are statically linked into
//       // the executable, so we have to use __Internal as the
//       // library name.
//       [DllImport ("__Internal")]

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

    private static IntPtr _xsapiNativeDll;
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

    private void Start() 
    {
        _xsapiNativeDll = LoadNativeDll(@"Microsoft.Xbox.Services.141.UWP.C.dll");
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xbl_signin_silently_result
    {
        public int errorCode;
        public int signInResultCode;
    }

    private delegate void xbl_signin_silently_completion_routine(IntPtr completionRoutineContext, xbl_signin_silently_result result);
    private delegate int xbl_signin_silently(IntPtr completionRoutineContext, xbl_signin_silently_completion_routine fn);
    private delegate void xbl_thread_process_pending_async_op();
    private delegate bool xbl_thread_is_async_op_pending();
    private delegate void xbl_thread_set_thread_pool_num_threads(long targetNumThreads);
    private delegate void xbl_thread_set_thread_ideal_processor(int threadIndex, uint dwIdealProcessor);
    private delegate bool xbl_thread_is_async_op_done(int handle);
    private delegate double xbl_get_version();
    private delegate bool xbl_thread_async_op_get_result(int handle, IntPtr result, int size);


    private void signin_complete(IntPtr completionRoutineContext, xbl_signin_silently_result result)
    {
        Debug.Log("Complete");
        Debug.Log("errorCode: " + result.errorCode);
        Debug.Log("signInResultCode: " + result.signInResultCode);
    }

    private T GetResultFromNative<T>(int handle)
    {
        var sizeInBytes = Marshal.SizeOf(typeof(T));
        IntPtr p = Marshal.AllocCoTaskMem(sizeInBytes);
        bool result = Invoke<bool, xbl_thread_async_op_get_result>(_xsapiNativeDll, handle, p, sizeInBytes);
        T res = (T)System.Runtime.InteropServices.Marshal.PtrToStructure(p, typeof(T));
        Marshal.FreeCoTaskMem(p);
        return res;
    }

    private bool doOnce = true;
    private void Update()
    {
        if (doOnce)
        {
            doOnce = false;

            Debug.Log("XBL Version: " + Invoke<double, xbl_get_version>(_xsapiNativeDll));
            Invoke<xbl_thread_set_thread_pool_num_threads>(_xsapiNativeDll, 0);
            int handle = Invoke<int, xbl_signin_silently>(_xsapiNativeDll, (IntPtr)3, (xbl_signin_silently_completion_routine)signin_complete);
            // int handle = Invoke<int, xbl_signin_silently>(_xsapiNativeDll, (IntPtr)3, null);
            bool pendingOp = Invoke<bool, xbl_thread_is_async_op_pending>(_xsapiNativeDll);
            Debug.Log("xbl_thread_is_async_op_pending: " + pendingOp);
            Invoke<xbl_thread_process_pending_async_op>(_xsapiNativeDll);
            //Invoke<xbl_thread_set_thread_pool_num_threads>(_xsapiNativeDll, 3);
            //Invoke<xbl_thread_set_thread_ideal_processor>(_xsapiNativeDll, 0, (uint)0);
            //bool isDone = Invoke<bool, xbl_thread_is_async_op_done>(_xsapiNativeDll, handle);
            //Debug.Log("xbl_thread_is_async_op_done: " + isDone);
            //xbl_signin_silently_result res = GetResultFromNative<xbl_signin_silently_result>(handle);
            //Debug.Log("errorCode: " + res.errorCode);
            //Debug.Log("signInResultCode: " + res.signInResultCode);

            Debug.Log("Done");
        }
    }
}
