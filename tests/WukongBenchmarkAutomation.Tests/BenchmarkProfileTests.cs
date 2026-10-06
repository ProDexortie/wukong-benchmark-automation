using WukongBenchmarkAutomation.Models;
using Xunit;

namespace WukongBenchmarkAutomation.Tests;

public class BenchmarkProfileTests
{
    [Fact]
    public void CreateCpuProfile_SetsMinimalGpuLoadAndMaxVegetation()
    {
        const int resX = 1280;
        const int resY = 720;
        const int nativeW = 2560;
        const int nativeH = 1440;

        var profile = BenchmarkProfile.CreateCpuProfile(resX, resY, nativeW, nativeH);

        Assert.Equal("CPU", profile.Name);
        Assert.Equal(1280, profile.ResolutionX);
        Assert.Equal(720, profile.ResolutionY);
        Assert.Equal("33.3", profile.ResolutionScale);
        Assert.Equal(1, profile.Settings["ShadowQuality"]);
        Assert.Equal(1, profile.Settings["TextureQuality"]);
        Assert.Equal(1, profile.Settings["AntiAliasing"]);
        Assert.Equal(0, profile.Settings["Rtx"]);
        Assert.Equal(0, profile.Settings["MotionBlur"]);
        Assert.Equal(180, profile.Settings["ImageQuality"]);
        Assert.Equal(2, profile.Settings["SuperResolutionSampling"]);
        Assert.Equal(5, profile.Settings["ViewDistance"]);
        Assert.Equal(5, profile.Settings["VegetationQuality"]);

        // Генерация кадров выключена
        Assert.Equal(0, profile.Settings["InsertFrame"]);
    }

    [Fact]
    public void CreateGpuProfile_SetsMaximalSettingsAndRayTracing()
    {
        const int nativeW = 2560;
        const int nativeH = 1440;
        var profile = BenchmarkProfile.CreateGpuProfile(nativeW, nativeH, rayTracing: true);

        Assert.Equal("GPU", profile.Name);
        Assert.Equal(nativeW, profile.ResolutionX);
        Assert.Equal(nativeH, profile.ResolutionY);
        Assert.Equal("100", profile.ResolutionScale);
        Assert.Equal(5, profile.Settings["ShadowQuality"]);
        Assert.Equal(5, profile.Settings["TextureQuality"]);
        Assert.Equal(5, profile.Settings["GlobalIllumination"]);
        Assert.Equal(5, profile.Settings["QualityLevel"]);
        Assert.Equal(nativeH, profile.Settings["ImageQuality"]); // 100% высота
        Assert.Equal(1, profile.Settings["Rtx"]);
        Assert.Equal(3, profile.Settings["RtxLevel"]);
        Assert.Equal(2, profile.Settings["MotionBlur"]);
        Assert.Equal(640, profile.StreamingPoolSize);
        Assert.Equal(0, profile.Settings["InsertFrame"]);
    }

    [Fact]
    public void CreateGpuProfile_DisablesRayTracing_WhenNotSupported()
    {
        // Когда RTX не поддерживается
        var profile = BenchmarkProfile.CreateGpuProfile(1920, 1080, rayTracing: false);

        Assert.Equal(0, profile.Settings["Rtx"]);
        Assert.Equal(1, profile.Settings["RtxLevel"]); // 1 = RtxOffLevel
    }
}
