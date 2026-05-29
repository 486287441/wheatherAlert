using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WeatherAlert.TrayPopup.Wpf.Chrome;

public static class PopupWindowHelper
{
    private const int GwlExstyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public static void EnableTrayPopupStyle(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var extendedStyle = GetWindowLong(handle, GwlExstyle);
            SetWindowLong(handle, GwlExstyle, extendedStyle | WsExNoActivate | WsExToolWindow);
        };
    }

    private static int GetWindowLong(IntPtr hwnd, int index) =>
        IntPtr.Size == 8
            ? (int)GetWindowLongPtr64(hwnd, index)
            : GetWindowLong32(hwnd, index);

    private static void SetWindowLong(IntPtr hwnd, int index, int value)
    {
        if (IntPtr.Size == 8)
        {
            _ = SetWindowLongPtr64(hwnd, index, new IntPtr(value));
        }
        else
        {
            _ = SetWindowLong32(hwnd, index, value);
        }
    }
}
