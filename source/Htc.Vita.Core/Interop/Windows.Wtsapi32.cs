﻿using System;
using System.Runtime.InteropServices;

namespace Htc.Vita.Core.Interop
{
    internal static partial class Windows
    {
        public static partial class Wtsapi32
        {
            public static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

            /**
             * https://msdn.microsoft.com/en-us/library/aa383860.aspx
             */
            public enum WTS_CONNECTSTATE_CLASS
            {
                WTSActive,
                WTSConnected,
                WTSConnectQuery,
                WTSShadow,
                WTSDisconnected,
                WTSIdle,
                WTSListen,
                WTSReset,
                WTSDown,
                WTSInit
            }

            /**
             * https://msdn.microsoft.com/en-us/library/aa383829.aspx
             */
            [DllImport(Libraries.Windows_wtsapi32,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            public static extern void WTSCloseServer(
                    [In] IntPtr hServer
            );

            /**
             * https://msdn.microsoft.com/en-us/library/aa383831.aspx
             */
            [DllImport(Libraries.Windows_wtsapi32,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool WTSEnumerateProcessesW(
                    [In] IntPtr hServer,
                    [In] [MarshalAs(UnmanagedType.U4)] int reserved,
                    [In] [MarshalAs(UnmanagedType.U4)] int version,
                    ref IntPtr ppProcessInfo,
                    [MarshalAs(UnmanagedType.U4)] ref int pCount
            );

            /**
             * https://msdn.microsoft.com/en-us/library/aa383833.aspx
             */
            [DllImport(Libraries.Windows_wtsapi32,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool WTSEnumerateSessionsW(
                    [In] IntPtr hServer,
                    [In] [MarshalAs(UnmanagedType.U4)] int reserved,
                    [In] [MarshalAs(UnmanagedType.U4)] int version,
                    ref IntPtr ppSessionInfo,
                    [MarshalAs(UnmanagedType.U4)] ref int pCount
            );
        }
    }
}
