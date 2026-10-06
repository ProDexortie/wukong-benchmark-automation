using System.Text.Json;

namespace WukongBenchmarkAutomation.Models;
public class BenchmarkResult
{
    // Метрики
    public long TimeStamp { get; set; }
    public double FPSAvg { get; set; }
    public double FPSMax { get; set; }
    public double FPSMin { get; set; }
    public double FPS95 { get; set; }
    public double VideoMem { get; set; }

    // Характеристики ПК

    public string SysVer { get; set; } = "";
    public string CPUModel { get; set; } = "";
    public string GPUModel { get; set; } = "";
    public string GpuDriverVer { get; set; } = "";
    public string VideoMemSize { get; set; } = "";
    public string SysMem { get; set; } = "";

    // Настройки с которыми был проведен бенчмарк
    public string ScreenResolution { get; set; } = "";
    public int QualityLevel { get; set; }
    public int ImageQuality { get; set; }
    public int ViewDistance { get; set; }
    public int AntiAliasing { get; set; }
    public int PostProcessing { get; set; }
    public int ShadowQuality { get; set; }
    public int TextureQuality { get; set; }
    public int MaterialQuality { get; set; }
    public int VegetationQuality { get; set; }
    public int MotionBlur { get; set; }
    public int Rtx { get; set; }
    public int Dlss { get; set; }
    public int InsertFrame { get; set; }
    public int Dx12 { get; set; }

    public static BenchmarkResult Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<BenchmarkResult>(json, options) ?? throw new JsonException ("Файл результата пустой");

        if (result.TimeStamp == 0 || result.FPSAvg == 0) 
            throw new JsonException("Файл результата некорректный: отсутствует метка времени или FPSAvg");

        result.CPUModel = result.CPUModel.Trim();
        return result;
    }
}
