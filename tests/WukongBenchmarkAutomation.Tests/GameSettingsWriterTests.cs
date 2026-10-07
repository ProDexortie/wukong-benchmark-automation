using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services;
using WukongBenchmarkAutomation.Services.Contracts;
using Xunit;

namespace WukongBenchmarkAutomation.Tests;

public class GameSettingsWriterTests : IDisposable
{
    private readonly string _testGameDir;
    private readonly string _iniDir;
    private readonly string _iniPath;

    private const string InitialIniContent = """
    [GameUserSettings]
    UISettingData=(("QualityLevel", "3"), ("ViewDistance", "3"), ("VegetationQuality", "3"), ("AntiAliasing", "3"), ("PostProcessing", "3"), ("ShadowQuality", "3"), ("TextureQuality", "3"), ("FxQuality", "3"), ("MaterialQuality", "3"), ("GlobalIllumination", "3"), ("ReflectionQuality", "3"), ("SuperResolutionSampling", "0"), ("ImageQuality", "1080"), ("InsertFrame", "0"), ("MotionBlur", "1"), ("Rtx", "0"), ("RtxLevel", "1"), ("Vsync", "1"), ("LockFrameRate", "1"), ("ScreenMode", "0"))
    ResolutionSizeX=1920
    ResolutionSizeY=1080
    LastUserConfirmedResolutionSizeX=1920
    LastUserConfirmedResolutionSizeY=1080
    LastUserConfirmedDesiredScreenWidth=1920
    LastUserConfirmedDesiredScreenHeight=1080
    bUseVSync=True
    FrameRateLimit=60.000000

    [ScalabilityGroups]
    sg.ResolutionQuality=100
    sg.ViewDistanceQuality=2
    sg.AntiAliasingQuality=2
    sg.ShadowQuality=2
    sg.PostProcessQuality=2
    sg.TextureQuality=2
    sg.EffectsQuality=2
    sg.FoliageQuality=2
    sg.ShadingQuality=2
    sg.GlobalIlluminationQuality=2
    sg.ReflectionQuality=2
    sg.RayTracingQuality=0

    [GSRenderSetting]
    GSStreamingPoolSize=384

    [RayTracing]
    r.RayTracing.EnableInGame=False
    """;

    public GameSettingsWriterTests()
    {
        _testGameDir = Path.Combine(Path.GetTempPath(), "WukongTest_" + Guid.NewGuid().ToString("N"));
        _iniDir = Path.Combine(_testGameDir, "b1", "Saved", "Config", "Windows");
        Directory.CreateDirectory(_iniDir);
        _iniPath = Path.Combine(_iniDir, "GameUserSettings.ini");
        File.WriteAllText(_iniPath, InitialIniContent);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testGameDir))
            {
                Directory.Delete(_testGameDir, recursive: true);
            }
        }
        catch
        {
            // Игнорируем ошибки очистки временных файлов
        }
    }

    [Fact]
    public void BackupAndRestore_RestoresOriginalContent()
    {
        var writer = new GameSettingsWriter(_testGameDir);

        writer.Backup();
        var backupPath = _iniPath + ".bak_wukong_automation";
        Assert.True(File.Exists(backupPath));

        // Замена содержимого ini-файла на что-то другое, чтобы проверить восстановление
        File.WriteAllText(_iniPath, "Corrupted content");
        Assert.NotEqual(InitialIniContent, File.ReadAllText(_iniPath));

        writer.Restore();

        Assert.Equal(InitialIniContent, File.ReadAllText(_iniPath));
        Assert.False(File.Exists(backupPath)); // Бэкап должен быть удален
    }

    [Fact]
    public void Apply_UpdatesResolutionAndQualityCorrectly()
    {
        var writer = new GameSettingsWriter(_testGameDir);
        var profile = BenchmarkProfile.CreateCpuProfile(1280, 720, 1920, 1080);

        writer.Apply(profile);
        var modifiedContent = File.ReadAllText(_iniPath);

        Assert.Contains("ResolutionSizeX=1280", modifiedContent);
        Assert.Contains("ResolutionSizeY=720", modifiedContent);
        Assert.Contains("bUseVSync=False", modifiedContent);
        Assert.Contains("FrameRateLimit=0.000000", modifiedContent);
        Assert.Contains("sg.ResolutionQuality=33.3", modifiedContent);
        Assert.Contains("sg.ViewDistanceQuality=4", modifiedContent); // ViewDistance 5 -> 5 - 1 = 4
        Assert.Contains("sg.FoliageQuality=4", modifiedContent);      // VegetationQuality 5 -> 4
        Assert.Contains("sg.ShadowQuality=0", modifiedContent);       // ShadowQuality 1 -> 0
    }

    [Fact]
    public void Backup_ThrowsFileNotFoundException_WhenIniDoesNotExist()
    {
        File.Delete(_iniPath);
        var writer = new GameSettingsWriter(_testGameDir);

        Assert.Throws<FileNotFoundException>(() => writer.Backup());
    }

    [Fact]
    public void Apply_WorksOnCleanIni_WithoutRayTracingAndRenderSettingSections()
    {
        // Абсолютно чистый файл после первого запуска 
        var cleanIni = """
        [/Script/GSGameSettings.GSGameUserSettings]
        UISettingData=(("QualityLevel", "6"),("ViewDistance", "4"),("AntiAliasing", "4"),("PostProcessing", "4"),("ShadowQuality", "4"),("TextureQuality", "4"),("FxQuality", "4"),("MaterialQuality", "4"),("VegetationQuality", "4"),("GlobalIllumination", "4"),("ReflectionQuality", "4"),("SuperResolutionSampling", "2"),("ImageQuality", "1080"),("InsertFrame", "0"),("MotionBlur", "2"),("Rtx", "0"),("RtxLevel", "2"),("Vsync", "0"),("LockFrameRate", "0"),("ScreenMode", "1"))
        ResolutionSizeX=2560
        ResolutionSizeY=1440
        LastUserConfirmedResolutionSizeX=2560
        LastUserConfirmedResolutionSizeY=1440
        LastUserConfirmedDesiredScreenWidth=1707
        LastUserConfirmedDesiredScreenHeight=960
        DesiredScreenWidth=1707
        DesiredScreenHeight=960
        bUseVSync=False
        FrameRateLimit=0.000000

        [ScalabilityGroups]
        sg.ResolutionQuality=66.6999969
        sg.ViewDistanceQuality=3
        sg.AntiAliasingQuality=3
        sg.ShadowQuality=3
        sg.GlobalIlluminationQuality=3
        sg.RayTracingQuality=0
        sg.ReflectionQuality=3
        sg.PostProcessQuality=3
        sg.TextureQuality=3
        sg.EffectsQuality=3
        sg.FoliageQuality=3
        sg.ShadingQuality=3
        """;

        File.WriteAllText(_iniPath, cleanIni);
        var writer = new GameSettingsWriter(_testGameDir);
        var profile = BenchmarkProfile.CreateCpuProfile(1280, 720, 2560, 1440);

        var exception = Record.Exception(() => writer.Apply(profile));
        Assert.Null(exception);

        var resultText = File.ReadAllText(_iniPath);
        Assert.Contains("r.RayTracing.EnableInGame=False", resultText);
        Assert.Contains("GSStreamingPoolSize=384", resultText);
        Assert.Contains("ResolutionSizeX=1280", resultText);
    }
}
