using System.Text;
using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services.Contracts;

namespace WukongBenchmarkAutomation.Services;

// Собирает итоговый отчёт в виде текста 
public class ReportPrinter : IReportPrinter
{
    string IReportPrinter.Build(BenchmarkResult cpu, BenchmarkResult gpu, BenchmarkProfile cpuProfile, BenchmarkProfile gpuProfile) =>
        Build(cpu, gpu, cpuProfile, gpuProfile);

    public static string Build(BenchmarkResult cpu, BenchmarkResult gpu, BenchmarkProfile cpuProfile, BenchmarkProfile gpuProfile)
    {
        var sb = new StringBuilder();
        var line = new string('=', 78);

        sb.AppendLine(line);
        sb.AppendLine("  BLACK MYTH: WUKONG BENCHMARK  ");
        sb.AppendLine(line);

        // Вывод характеристик ПК
        sb.AppendLine();
        sb.AppendLine("ХАРАКТЕРИСТИКИ КОМПЬЮТЕРА");
        sb.AppendLine($"  CPU:              {gpu.CPUModel} ({Environment.ProcessorCount} потоков)");
        sb.AppendLine($"  GPU:              {gpu.GPUModel}, {gpu.VideoMemSize} VRAM, драйвер {gpu.GpuDriverVer}");
        sb.AppendLine($"  RAM:              {gpu.SysMem}");
        sb.AppendLine($"  ОС:               Windows {gpu.SysVer}");

        // Вывод результатов
        sb.AppendLine();
        sb.AppendLine("РЕЗУЛЬТАТЫ");
        sb.AppendLine($"  {"Метрика",-34}{"CPU-тест",22}{"GPU-тест",22}");
        sb.AppendLine("  " + new string('-', 70));
        Row(sb, "Средний FPS", cpu.FPSAvg, gpu.FPSAvg, "0");
        Row(sb, "Максимальный FPS", cpu.FPSMax, gpu.FPSMax, "0");
        Row(sb, "Минимальный FPS", cpu.FPSMin, gpu.FPSMin, "0");
        Row(sb, "FPS 95-й перцентиль", cpu.FPS95, gpu.FPS95, "0");
        Row(sb, "Использование VRAM, ГБ", cpu.VideoMem, gpu.VideoMem, "0.0");

        // Вывод настроек игры
        sb.AppendLine();
        sb.AppendLine("НАСТРОЙКИ");
        sb.AppendLine($"  {"Параметр",-34}{"CPU-тест",22}{"GPU-тест",22}");
        sb.AppendLine("  " + new string('-', 70));
        Text(sb, "Разрешение", cpu.ScreenResolution, gpu.ScreenResolution);
        Text(sb, "Разрешение рендеринга (ImageQuality)", cpu.ImageQuality.ToString(), gpu.ImageQuality.ToString());
        Text(sb, "Общий уровень качества", FormatPreset(cpu.QualityLevel), FormatPreset(gpu.QualityLevel));
        Text(sb, "Дальность прорисовки", FormatQuality(cpu.ViewDistance), FormatQuality(gpu.ViewDistance));
        Text(sb, "Сглаживание", FormatQuality(cpu.AntiAliasing), FormatQuality(gpu.AntiAliasing));
        Text(sb, "Постобработка", FormatQuality(cpu.PostProcessing), FormatQuality(gpu.PostProcessing));
        Text(sb, "Тени", FormatQuality(cpu.ShadowQuality), FormatQuality(gpu.ShadowQuality));
        Text(sb, "Текстуры", FormatQuality(cpu.TextureQuality), FormatQuality(gpu.TextureQuality));
        Text(sb, "Материалы", FormatQuality(cpu.MaterialQuality), FormatQuality(gpu.MaterialQuality));
        Text(sb, "Растительность", FormatQuality(cpu.VegetationQuality), FormatQuality(gpu.VegetationQuality));
        Text(sb, "Размытие в движении", OnOff(cpu.MotionBlur), OnOff(gpu.MotionBlur));
        Text(sb, "Трассировка лучей", FormatRayTracing(cpu.Rtx), FormatRayTracing(gpu.Rtx));
        Text(sb, "DLSS / апскейл", OnOff(cpu.Dlss), OnOff(gpu.Dlss));
        Text(sb, "Генерация кадров", OnOff(cpu.InsertFrame), OnOff(gpu.InsertFrame));
        Text(sb, "DirectX 12", OnOff(cpu.Dx12), OnOff(gpu.Dx12));

        // Предупреждение на случай, если настройки игры расходятся с теми, которые записывались в ini
        var warnings = CheckApplied(cpu, cpuProfile).Concat(CheckApplied(gpu, gpuProfile)).ToList();
        if (warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ВНИМАНИЕ: игра применила не все запрошенные настройки:");
            foreach (var w in warnings) sb.AppendLine("  - " + w);
        }

        return sb.ToString();
    }

    // Сравнивает запрошенные значения профиля с тем, что записано в файле результата
    private static List<string> CheckApplied(BenchmarkResult result, BenchmarkProfile profile)
    {
        // ImageQuality в файле результата игра пересчитывает по своей формуле, поэтому его не сравниваем
        // Rtx в файле результата хранит уровень качества трассировки (RtxLevel), а не просто 0/1
        var actual = new Dictionary<string, int>
        {
            ["QualityLevel"] = result.QualityLevel,
            ["ViewDistance"] = result.ViewDistance,
            ["AntiAliasing"] = result.AntiAliasing,
            ["PostProcessing"] = result.PostProcessing,
            ["ShadowQuality"] = result.ShadowQuality,
            ["TextureQuality"] = result.TextureQuality,
            ["MaterialQuality"] = result.MaterialQuality,
            ["VegetationQuality"] = result.VegetationQuality,
            ["MotionBlur"] = result.MotionBlur,
            ["Rtx"] = result.Rtx,
            ["SuperResolutionSampling"] = result.Dlss, // игра пишет режим апскейла в поле Dlss
            ["InsertFrame"] = result.InsertFrame,
        };

        var problems = new List<string>();
        foreach (var (key, value) in profile.Settings)
        {
            var expected = key == "Rtx" && value == 1 ? profile.Settings["RtxLevel"] : value;
            if (actual.TryGetValue(key, out var got) && got != expected)
                problems.Add($"{profile.Name}-тест: {key} запрошено {expected}, в результате {got}");
        }

        if (result.ScreenResolution.Replace(" ", "") != $"{profile.ResolutionX}×{profile.ResolutionY}")
            problems.Add($"{profile.Name}-тест: разрешение запрошено {profile.ResolutionX}x{profile.ResolutionY}, в результате {result.ScreenResolution}");

        return problems;
    }

    private static void Row(StringBuilder sb, string name, double cpu, double gpu, string format) =>
        sb.AppendLine($"  {name,-34}{cpu.ToString(format),22}{gpu.ToString(format),22}");

    private static void Text(StringBuilder sb, string name, string cpu, string gpu) =>
        sb.AppendLine($"  {name,-34}{cpu,22}{gpu,22}");

    private static string OnOff(int value) => value == 0 ? "выкл" : $"вкл ({value})";

    private static string FormatQuality(int level) => level switch
    {
        1 => "Низкое",
        2 => "Среднее",
        3 => "Высокое",
        4 => "Ультра",
        5 => "Реалистичное",
        _ => level.ToString()
    };

    private static string FormatPreset(int level) => level switch
    {
        6 => "Пользовательское",
        _ => FormatQuality(level)
    };

    private static string FormatRayTracing(int rtxLevel) => rtxLevel switch
    {
        0 => "Выкл",
        1 => "Вкл (Низкое)",
        2 => "Вкл (Среднее)",
        3 => "Вкл (Ультра)",
        _ => $"Вкл ({rtxLevel})"
    };
}
