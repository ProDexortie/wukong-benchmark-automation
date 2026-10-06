namespace WukongBenchmarkAutomation.Models;

// Модель набора настроек для одного прохода бенчмарка. Ключи Settings совпадают с ключами в UISettingData из GameUserSettings.ini
// Профили собираются одним методом Create и задают один и тот же набор ключей во избежание искажения результатов теста
public class BenchmarkProfile
{
public string Name { get; set; }
// Разрешение под ключи из GameUserSettings.ini
public int ResolutionX { get; init; }
public int ResolutionY { get; init; }
// Разрешение, в которое игра считает DLSS/FSR
public int DesiredWidth { get; init; }
public int DesiredHeight { get; init; }

// GSStreamingPoolSize из [GSRenderSetting] в GameUserSettings.ini: 384 при низком качестве текстур, 640 при максимальном
public int StreamingPoolSize { get; init; }

public Dictionary<string, int> Settings { get; init; } = new();
// sg.ResolutionQuality из [ScalabilityGroups] в GameUserSettings.ini; 100 - при полном разрешении, 33 при DLSS 25%
public string ResolutionScale { get; init; } = "100";

private const int RtxOffLevel = 1;

// Профиль для CPU-теста
public static BenchmarkProfile CreateCpuProfile(int resolutionX, int resolutionY, int nativeWidth, int nativeHeight) => Create(
    name: "CPU",
    resolutionX: resolutionX, resolutionY: resolutionY, nativeWidth, nativeHeight,
    quality: 1, viewDistance: 5, vegetation: 5, qualityLevel: 6,
    renderScalePercent: 25, motionBlur: 0, rayTracingLevel: 0);

// Профиль для GPU-теста
public static BenchmarkProfile CreateGpuProfile(int nativeWidth, int nativeHeight, bool rayTracing) => Create(
    name: "GPU",
    resolutionX: nativeWidth, resolutionY: nativeHeight, nativeWidth, nativeHeight,
    quality: 5, viewDistance: 5, vegetation: 5, qualityLevel: 5,
    renderScalePercent: 100, motionBlur: 2, rayTracingLevel: rayTracing ? 3 : 0);
    
// Метод для унифицирования настроек, которые изменяют профили
private static BenchmarkProfile Create(string name, int resolutionX, int resolutionY, int nativeWidth, int nativeHeight,
    int quality, int viewDistance, int vegetation, int qualityLevel,
    int renderScalePercent, int motionBlur, int rayTracingLevel)
{
    // Настройки 1 = "Низкий" ... 5 = "Реалистичный"; QualityLevel 6 это "Пользовательские"
    var settings = new Dictionary<string, int>
    {
        ["QualityLevel"] = qualityLevel,
        ["ViewDistance"] = viewDistance,
        ["VegetationQuality"] = vegetation,
        ["AntiAliasing"] = quality,
        ["PostProcessing"] = quality,
        ["ShadowQuality"] = quality,
        ["TextureQuality"] = quality,
        ["FxQuality"] = quality,
        ["MaterialQuality"] = quality,
        ["GlobalIllumination"] = quality,
        ["ReflectionQuality"] = quality,
        // DLSS будет всегда включен (2), внутреннее разрешение рендера задает ImageQuality
        ["SuperResolutionSampling"] = 2,
        ["ImageQuality"] = resolutionY * renderScalePercent / 100,

        ["InsertFrame"] = 0, // генерация кадров выключена, дабы избежать влияния на показатели FPS
        ["MotionBlur"] = motionBlur,
        ["Rtx"] = rayTracingLevel > 0 ? 1 : 0,
        ["RtxLevel"] = rayTracingLevel > 0 ? rayTracingLevel : RtxOffLevel,
    };

    return new BenchmarkProfile
    {
        Name = name,
        ResolutionX = resolutionX,
        ResolutionY = resolutionY,
        Settings = settings,
        ResolutionScale = renderScalePercent == 100 ? "100" : "33.3",
        DesiredWidth = renderScalePercent == 100 ? nativeWidth : (int)(nativeWidth * 0.333),
        DesiredHeight = renderScalePercent == 100 ? nativeHeight : (int)(nativeHeight * 0.333),
        StreamingPoolSize = quality >= 5 ? 640 : 384,
    };
}
}
