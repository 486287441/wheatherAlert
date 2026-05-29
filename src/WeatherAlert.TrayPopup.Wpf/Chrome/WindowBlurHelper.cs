using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WeatherAlert.TrayPopup.Wpf.Chrome;

public static class WindowBlurHelper
{
    public const int DefaultCornerRadius = 16;
    public const double DefaultFlyoutWidth = 360;
    public const double DefaultFlyoutHeight = 420;
    public const double DefaultMenuWidth = 280;
    public const double DefaultMenuHeight = 268;

    private const int DwmwaSystemBackdropType = 38;
    private const int DwmsbtTransientWindow = 3;
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    public static bool TryEnableBlur(Window window)
    {
        if (window is null)
        {
            return false;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        return TryEnableBlur(handle);
    }

    public static bool TryEnableBlur(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var backdrop = DwmsbtTransientWindow;
        if (DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref backdrop, sizeof(int)) == 0)
        {
            return true;
        }

        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        _ = DwmExtendFrameIntoClientArea(hwnd, ref margins);

        var dark = 0;
        _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
        return true;
    }
}
