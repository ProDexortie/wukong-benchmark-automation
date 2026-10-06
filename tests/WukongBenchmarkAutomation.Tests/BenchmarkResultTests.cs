using System.Text.Json;
using WukongBenchmarkAutomation.Models;
using Xunit;

namespace WukongBenchmarkAutomation.Tests;

public class BenchmarkResultTests
{
    private const string SampleJson = """
    {
        "TimeStamp": 1791269089,
        "FPSAvg": 144.5,
        "FPSMax": 175.0,
        "FPSMin": 71.0,
        "FPS95": 133.0,
        "CPUAvg": 1.0,
        "GPUAvg": 33.0,
        "VideoMem": 4.5,
        "GameVer": "1.0.3.14649",
        "SysVer": "Windows 11",
        "CPUModel": "AMD Ryzen 7 5800X3D   ",
        "GPUModel": "NVIDIA GeForce RTX 4070 Ti SUPER",
        "GpuDriverVer": "560.81",
        "VideoMemSize": "16GB",
        "SysMem": "32GB",
        "ScreenMode": 1,
        "ScreenResolution": "1280 × 720",
        "QualityLevel": 6,
        "ImageQuality": 180,
        "ViewDistance": 5,
        "AntiAliasing": 1,
        "PostProcessing": 1,
        "ShadowQuality": 1,
        "TextureQuality": 1,
        "MaterialQuality": 1,
        "VegetationQuality": 5,
        "MotionBlur": 0,
        "Rtx": 0,
        "Dlss": 2,
        "InsertFrame": 0,
        "Dx12": 1
    }
    """;

    [Fact]
    public void Parse_ValidJson_ReturnsPopulatedResult()
    {
        var result = BenchmarkResult.Parse(SampleJson);

        Assert.Equal(1791269089, result.TimeStamp);
        Assert.Equal(144.5, result.FPSAvg);
        Assert.Equal(175.0, result.FPSMax);
        Assert.Equal(71.0, result.FPSMin);
        Assert.Equal(133.0, result.FPS95);
        Assert.Equal(4.5, result.VideoMem);
        Assert.Equal("AMD Ryzen 7 5800X3D", result.CPUModel); // Проверка срабатывания .Trim()
        Assert.Equal("NVIDIA GeForce RTX 4070 Ti SUPER", result.GPUModel);
        Assert.Equal("1280 × 720", result.ScreenResolution);
        Assert.Equal(2, result.Dlss);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => BenchmarkResult.Parse("not-a-valid-json"));
    }

    [Fact]
    public void Parse_MissingFpsAvg_ThrowsJsonException()
    {
        var invalidJson = """
        {
            "TimeStamp": 1791269089,
            "FPSAvg": 0,
            "CPUModel": "AMD Ryzen"
        }
        """;

        Assert.Throws<JsonException>(() => BenchmarkResult.Parse(invalidJson));
    }

    [Fact]
    public void Parse_MissingTimeStamp_ThrowsJsonException()
    {
        var invalidJson = """
        {
            "TimeStamp": 0,
            "FPSAvg": 60,
            "CPUModel": "Intel Core i7"
        }
        """;

        Assert.Throws<JsonException>(() => BenchmarkResult.Parse(invalidJson));
    }
}
