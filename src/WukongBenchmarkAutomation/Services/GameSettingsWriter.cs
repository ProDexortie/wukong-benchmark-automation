using System.Text.RegularExpressions;
using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services.Contracts;

namespace WukongBenchmarkAutomation.Services;

// Изменяет GameUserSettings.ini бенчмарка под выбранный профиль
public class GameSettingsWriter : IGameSettingsWriter
{
    private readonly string _iniPath;
    private readonly string _backupPath;

    public GameSettingsWriter(string gameDir)
    {
        _iniPath = Path.Combine(gameDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
        _backupPath = _iniPath + ".bak_wukong_automation";
    }

    // ключ в меню игры -> ключ в секции [ScalabilityGroups]
    private static readonly Dictionary<string, string> ScalabilityMap = new()
    {
        ["ViewDistance"] = "sg.ViewDistanceQuality",
        ["AntiAliasing"] = "sg.AntiAliasingQuality",
        ["PostProcessing"] = "sg.PostProcessQuality",
        ["ShadowQuality"] = "sg.ShadowQuality",
        ["TextureQuality"] = "sg.TextureQuality",
        ["FxQuality"] = "sg.EffectsQuality",
        ["MaterialQuality"] = "sg.ShadingQuality",
        ["VegetationQuality"] = "sg.FoliageQuality",
        ["GlobalIllumination"] = "sg.GlobalIlluminationQuality",
        ["ReflectionQuality"] = "sg.ReflectionQuality",
    };

    public string IniPath => _iniPath;

    // Сохраняет оригинальные настройки пользователя, чтобы вернуть их после прогонов бенчмарков
    public void Backup()
    {
        if (!File.Exists(_iniPath))
            throw new FileNotFoundException(
                $"Не найден {_iniPath}. Запустите бенчмарк один раз вручную, чтобы игра создала файл настроек.");

        File.Copy(_iniPath, _backupPath, overwrite: true);
    }

    public void Restore()
    {
        if (!File.Exists(_backupPath)) return;
        File.Copy(_backupPath, _iniPath, overwrite: true);
        File.Delete(_backupPath);
    }

    public void Apply(BenchmarkProfile profile)
    {
        var text = File.ReadAllText(_iniPath);

        // ImageQuality в UISettingData = внутреннее разрешение рендеринга в пикселях, SuperResolutionSampling = режим DLSS
        foreach (var (key, value) in profile.Settings)
            text = SetUiSetting(text, key, value.ToString());

        // Игра согласовывает параметры sg.* с UISettingData
        foreach (var (uiKey, sgKey) in ScalabilityMap)
        {
            if (profile.Settings.TryGetValue(uiKey, out var level))
                text = SetLine(text, sgKey, (level - 1).ToString());
        }
        text = SetLine(text, "sg.ResolutionQuality", profile.ResolutionScale);

        var rayTracing = profile.Settings.GetValueOrDefault("Rtx") == 1;
        text = SetLine(text, "sg.RayTracingQuality", "0"); // игра сама держит здесь 0 даже при включённой трассировке (уровень хранится в RtxLevel)
        text = SetSectionProperty(text, "RayTracing", "r.RayTracing.EnableInGame", rayTracing ? "True" : "False");

        // Разрешение, отключение Vsync и лимита FPS
        text = SetUiSetting(text, "Vsync", "0");
        text = SetUiSetting(text, "LockFrameRate", "0");
        text = SetUiSetting(text, "ScreenMode", "1"); // 1 = полноэкранный без рамки
        text = SetLine(text, "ResolutionSizeX", profile.ResolutionX.ToString());
        text = SetLine(text, "ResolutionSizeY", profile.ResolutionY.ToString());
        text = SetLine(text, "LastUserConfirmedResolutionSizeX", profile.ResolutionX.ToString());
        text = SetLine(text, "LastUserConfirmedResolutionSizeY", profile.ResolutionY.ToString());
        text = SetLine(text, "LastUserConfirmedDesiredScreenWidth", profile.DesiredWidth.ToString());
        text = SetLine(text, "LastUserConfirmedDesiredScreenHeight", profile.DesiredHeight.ToString());
        text = SetLineOrInsert(text, "DesiredScreenWidth", profile.DesiredWidth.ToString(), after: "LastUserConfirmedDesiredScreenHeight");
        text = SetLineOrInsert(text, "DesiredScreenHeight", profile.DesiredHeight.ToString(), after: "DesiredScreenWidth");
        text = SetSectionProperty(text, "GSRenderSetting", "GSStreamingPoolSize", profile.StreamingPoolSize.ToString());
        text = SetLine(text, "bUseVSync", "False");
        text = SetLine(text, "FrameRateLimit", "0.000000");

        File.WriteAllText(_iniPath, text);
    }

    // ("Key", "Value") -> ("Key", "новое значение")
    private static string SetUiSetting(string text, string key, string value)
    {
        var pattern = $"\\(\"{Regex.Escape(key)}\",\\s*\"[^\"]*\"\\)";
        if (!Regex.IsMatch(text, pattern))
            throw new InvalidOperationException($"В UISettingData нет параметра {key}");

        return Regex.Replace(text, pattern, $"(\"{key}\", \"{value}\")");
    }

    // Key=Value (целая строка)
    private static string SetLine(string text, string key, string value)
    {
        // [^\r\n]* вместо .*$ иначе из-за \r\n в конце строки совпадение может быть пропущено
        var pattern = $"^{Regex.Escape(key)}=[^\r\n]*";
        if (!Regex.IsMatch(text, pattern, RegexOptions.Multiline))
            throw new InvalidOperationException($"В GameUserSettings.ini нет строки {key}");

        return Regex.Replace(text, pattern, $"{key}={value}", RegexOptions.Multiline);
    }

    // Повтор SetLine, но если в исходном ini ее не, добавляет ее после строки after
    private static string SetLineOrInsert(string text, string key, string value, string after)
    {
        if (Regex.IsMatch(text, $"^{Regex.Escape(key)}=", RegexOptions.Multiline))
            return SetLine(text, key, value);

        var afterPattern = $"^{Regex.Escape(after)}=[^\r\n]*";
        if (!Regex.IsMatch(text, afterPattern, RegexOptions.Multiline))
            throw new InvalidOperationException($"В GameUserSettings.ini нет строки {after}");

        return Regex.Replace(text, afterPattern, m => $"{m.Value}\r\n{key}={value}", RegexOptions.Multiline);
    }

    // Обновляет Key=Value в указанной секции [Section], либо создает секцию/ключ, если их нет в файле
    private static string SetSectionProperty(string text, string section, string key, string value)
    {
        var keyPattern = $"^{Regex.Escape(key)}=[^\r\n]*";
        if (Regex.IsMatch(text, keyPattern, RegexOptions.Multiline))
            return Regex.Replace(text, keyPattern, $"{key}={value}", RegexOptions.Multiline);

        var sectionPattern = $"^\\[{Regex.Escape(section)}\\][^\r\n]*";
        if (Regex.IsMatch(text, sectionPattern, RegexOptions.Multiline))
            return Regex.Replace(text, sectionPattern, m => $"{m.Value}\r\n{key}={value}", RegexOptions.Multiline);

        var trimmed = text.TrimEnd();
        return $"{trimmed}\r\n\r\n[{section}]\r\n{key}={value}\r\n";
    }
}
