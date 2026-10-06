namespace WukongBenchmarkAutomation.Services.Contracts;

// Интерфейс для определения параметров дисплея

public interface IDisplayHelper
{
    (int Width, int Height) GetNativeResolution();
}
