﻿using System;
using System.Runtime.InteropServices;

namespace Htc.Vita.Core.Interop
{
    internal static partial class Windows
    {
        public static partial class Setupapi
        {
            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551069.aspx
             */
            [Flags]
            public enum DIGCF
            {
                DIGCF_DEFAULT = 0x00000001,
                DIGCF_PRESENT = 0x00000002,
                DIGCF_ALLCLASSES = 0x00000004,
                DIGCF_PROFILE = 0x00000008,
                DIGCF_DEVICEINTERFACE = 0x00000010
            }

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff552342.aspx
             */
            [StructLayout(LayoutKind.Sequential)]
            public struct SP_DEVICE_INTERFACE_DATA
            {
                public int cbSize;

                public Guid interfaceClassGuid;

                public int flags;

                public IntPtr reserved;
            }

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff550996.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetupDiDestroyDeviceInfoList(
                    IntPtr deviceInfoSet
            );

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551015.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetupDiEnumDeviceInterfaces(
                    IntPtr deviceInfoSet,
                    IntPtr deviceInfoData,
                    ref Guid interfaceClassGuid,
                    int memberIndex,
                    ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
            );

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551069.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            public static extern IntPtr SetupDiGetClassDevsW(
                    ref Guid classGuid,
                    [MarshalAs(UnmanagedType.LPTStr)] string enumerator,
                    IntPtr hwndParent,
                    DIGCF flags
            );

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551120.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetupDiGetDeviceInterfaceDetailW(
                    IntPtr hDevInfo,
                    ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
                    IntPtr deviceInterfaceDetailData,
                    int deviceInterfaceDetailDataSize,
                    ref int requiredSize,
                    IntPtr deviceInfoData
            );

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551120.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetupDiGetDeviceInterfaceDetailW(
                    IntPtr hDevInfo,
                    ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
                    IntPtr deviceInterfaceDetailData,
                    int deviceInterfaceDetailDataSize,
                    ref int requiredSize,
                    ref SP_DEVINFO_DATA deviceInfoData
            );

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551967.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetupDiGetDeviceRegistryPropertyW(
                    IntPtr deviceInfoSet,
                    ref SP_DEVINFO_DATA deviceInfoData,
                    SPDRP property,
                    IntPtr propertyRegDataType,
                    IntPtr propertyBuffer,
                    int propertyBufferSize,
                    out int requiredSize
            );

            /**
             * https://msdn.microsoft.com/en-us/library/windows/hardware/ff551967.aspx
             */
            [DllImport(Libraries.Windows_setupapi,
                    CallingConvention = CallingConvention.Winapi,
                    CharSet = CharSet.Unicode,
                    ExactSpelling = true,
                    SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetupDiGetDeviceRegistryPropertyW(
                    IntPtr deviceInfoSet,
                    ref SP_DEVINFO_DATA deviceInfoData,
                    SPDRP property,
                    out int propertyRegDataType,
                    byte[] propertyBuffer,
                    int propertyBufferSize,
                    out int requiredSize
            );
        }
    }
}
