using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JkUsbApp.Native
{
    internal enum UsbRelayDeviceType : int
    {
        One = 1,
        Two = 2,
        Four = 4,
        Eight = 8
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UsbRelayDeviceInfo
    {
        public IntPtr serial_number; // unsigned char*
        public IntPtr device_path;   // char*
        public UsbRelayDeviceType type;
        public IntPtr next;          // usb_relay_device_info*
    }

    internal static class UsbRelayNative
    {
        private const string DllName = "usb_relay_device.dll";

      
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_init();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_exit();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr usb_relay_device_enumerate();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void usb_relay_device_free_enumerate(IntPtr deviceInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_device_open(IntPtr deviceInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void usb_relay_device_close(int hHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_device_open_one_relay_channel(int hHandle, int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_device_close_one_relay_channel(int hHandle, int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_device_open_all_relay_channel(int hHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_device_close_all_relay_channel(int hHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int usb_relay_device_get_status(int hHandle, out uint status);
    }

    internal static class UsbRelayHelpers
    {
        private static string PtrToAnsi(IntPtr p) =>
            p == IntPtr.Zero ? "" : (Marshal.PtrToStringAnsi(p) ?? "");

    
        public static List<(string serial, IntPtr nodePtr, UsbRelayDeviceType type)> Enumerate(out IntPtr listHead)
        {
            listHead = UsbRelayNative.usb_relay_device_enumerate();
            var result = new List<(string, IntPtr, UsbRelayDeviceType)>();

            IntPtr cur = listHead;
            while (cur != IntPtr.Zero)
            {
                var info = Marshal.PtrToStructure<UsbRelayDeviceInfo>(cur);
                string serial = PtrToAnsi(info.serial_number);
                result.Add((serial, cur, info.type));
                cur = info.next;
            }

            return result;
        }

        public static int ChannelCount(UsbRelayDeviceType type) => (int)type; // 1/2/4/8
    }
}
