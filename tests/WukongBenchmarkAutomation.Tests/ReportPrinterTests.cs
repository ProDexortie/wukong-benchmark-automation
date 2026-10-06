using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services;
using Xunit;

namespace WukongBenchmarkAutomation.Tests;

public class ReportPrinterTests
{
    private static BenchmarkResult CreateSampleResult(double fpsAvg, string resolution, int qualityLevel, int rtx)
    {
        return new BenchmarkResult
        {
            TimeStamp = 1791269089,
            FPSAvg = fpsAvg,
            FPSMax = fpsAvg + 20,
            FPSMin = fpsAvg - 20,
            FPS95 = fpsAvg - 5,
            VideoMem = 6.2,
            CPUModel = "AMD Ryzen 7 5800X3D",
            GPUModel = "NVIDIA GeForce RTX 4070 Ti SUPER",
            GpuDriverVer = "560.81",
            VideoMemSize = "16GB",
            SysMem = "32GB",
            SysVer = "Windows 11",
            ScreenResolution = resolution,
            QualityLevel = qualityLevel,
            ViewDistance = 5,
            AntiAliasing = qualityLevel == 6 ? 1 : 5,
            PostProcessing = qualityLevel == 6 ? 1 : 5,
            ShadowQuality = qualityLevel == 6 ? 1 : 5,
            TextureQuality = qualityLevel == 6 ? 1 : 5,
            MaterialQuality = qualityLevel == 6 ? 1 : 5,
            VegetationQuality = 5,
            MotionBlur = qualityLevel == 6 ? 0 : 2,
            Rtx = rtx,
            Dlss = 2,
            InsertFrame = 0,
            Dx12 = 1,
            ImageQuality = 180
        };
    }

    [Fact]
    public void Build_ContainsHardwareAndBothResults()
    {
        var cpuProfile = BenchmarkProfile.CreateCpuProfile(1280, 720, 2560, 1440);
        var gpuProfile = BenchmarkProfile.CreateGpuProfile(2560, 1440, rayTracing: true);

        var cpuResult = CreateSampleResult(194.0, "1280×720", qualityLevel: 6, rtx: 0);
        var gpuResult = CreateSampleResult(27.0, "2560×1440", qualityLevel: 5, rtx: 3);

        var report = ReportPrinter.Build(cpuResult, gpuResult, cpuProfile, gpuProfile);

        Assert.Contains("BLACK MYTH: WUKONG BENCHMARK", report);
        Assert.Contains("AMD Ryzen 7 5800X3D", report);
        Assert.Contains("NVIDIA GeForce RTX 4070 Ti SUPER", report);
        Assert.Contains("Средний FPS", report);
        Assert.Contains("194", report);
        Assert.Contains("27", report);
        Assert.Contains("НАСТРОЙКИ", report);
        Assert.DoesNotContain("ВНИМАНИЕ:", report); // все настройки совпали, предупреждений нет
    }

    [Fact]
    public void Build_WhenSettingMismatch_AppendsWarningsSection()
    {
        var cpuProfile = BenchmarkProfile.CreateCpuProfile(1280, 720, 2560, 1440);
        var gpuProfile = BenchmarkProfile.CreateGpuProfile(2560, 1440, rayTracing: true);

        // Игра вместо 1280x720 применила 1920x1080
        var cpuResult = CreateSampleResult(120.0, "1920×1080", qualityLevel: 6, rtx: 0);
        var gpuResult = CreateSampleResult(27.0, "2560×1440", qualityLevel: 5, rtx: 3);

        var report = ReportPrinter.Build(cpuResult, gpuResult, cpuProfile, gpuProfile);

        Assert.Contains("ВНИМАНИЕ: игра применила не все запрошенные настройки:", report);
        Assert.Contains("CPU-тест: разрешение запрошено 1280x720, в результате 1920×1080", report);
    }

    [Fact]
    public void Build_FormatsQualityLevelsAsHumanReadableStrings()
    {
        var cpuProfile = BenchmarkProfile.CreateCpuProfile(1280, 720, 2560, 1440);
        var gpuProfile = BenchmarkProfile.CreateGpuProfile(2560, 1440, rayTracing: true);

        var cpuResult = CreateSampleResult(194.0, "1280×720", qualityLevel: 6, rtx: 0);
        var gpuResult = CreateSampleResult(27.0, "2560×1440", qualityLevel: 5, rtx: 3);

        var report = ReportPrinter.Build(cpuResult, gpuResult, cpuProfile, gpuProfile);

        Assert.Contains("Пользовательское", report);
        Assert.Contains("Реалистичное", report);
        Assert.Contains("Низкое", report);
        Assert.Contains("Вкл (Ультра)", report);
    }
}
