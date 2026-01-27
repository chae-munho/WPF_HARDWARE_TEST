using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

public static class NativeDllCheck
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static bool CheckAndLoadUioDll()
    {
        // 실행 폴더 기준으로 확인
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uio.dll");

        if (!File.Exists(dllPath))
        {
            MessageBox.Show($"uio.dll이 실행 폴더에 없습니다.\n경로: {dllPath}");
            return false;
        }

        IntPtr h = LoadLibrary(dllPath);
        if (h == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            MessageBox.Show($"uio.dll 로드 실패 (LoadLibrary)\nWin32Error={err}\n{new Win32Exception(err).Message}");
            return false;
        }

        // 로드 성공
        FreeLibrary(h);
        MessageBox.Show("uio.dll 로드 성공 (LoadLibrary OK)");
        return true;
    }
}
