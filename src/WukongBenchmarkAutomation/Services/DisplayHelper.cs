using System.Runtime.InteropServices;
using WukongBenchmarkAutomation.Services.Contracts;

namespace WukongBenchmarkAutomation.Services;

// Определяет текущее разрешение основного монитора для проведения бенчмарка в нативном разрешении
public class DisplayHelper : IDisplayHelper
{
    (int Width, int Height) IDisplayHelper.GetNativeResolution() => GetNativeResolution();
    // Environment/Screen дают «масштабированное» разрешение при включённом масштабировании Windows (125%, 150%),
    // а EnumDisplaySettings возвращает реальные пиксели
    public static (int Width, int Height) GetNativeResolution()
    {
        var mode = new DevMode { dmSize = (short)Marshal.SizeOf<DevMode>() };
        if (EnumDisplaySettings(null, -1, ref mode) && mode.dmPelsWidth > 0)
            return (mode.dmPelsWidth, mode.dmPelsHeight);

        return (1920, 1080); // запасной вариант
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DevMode devMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}
