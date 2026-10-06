using System.Text;
using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services;

Console.OutputEncoding = Encoding.UTF8;

try
{
    // Необязательный параметр: --game-dir "путь", чтобы вручную указать директорию к бенчмарку
    var gameDirArg = GetArg(args, "--game-dir");

    Console.WriteLine("Поиск Steam и бенчмарка...");
    var steamExe = SteamLocator.FindSteamExe();
    var gameDir = gameDirArg ?? SteamLocator.FindGameDir(steamExe);
    Console.WriteLine($"      Steam: {steamExe}");
    Console.WriteLine($"      Игра:  {gameDir}");


    var runner = new BenchmarkRunner(steamExe, gameDir);
    if (runner.IsGameRunning())
        throw new InvalidOperationException("Бенчмарк уже запущен. Закройте его и запустите инструмент заново.");

    var settingsWriter = new GameSettingsWriter(gameDir);
    settingsWriter.Backup();

    BenchmarkResult cpuResult, gpuResult;
    BenchmarkProfile gpuProfile;
    var (nativeWidth, nativeHeight) = DisplayHelper.GetNativeResolution();
    var cpuProfile = BenchmarkProfile.CreateCpuProfile(1280, 720, nativeWidth, nativeHeight);

    try
    {
        Console.WriteLine("Происходит прогон CPU-теста...");
        settingsWriter.Apply(cpuProfile);
        cpuResult = runner.Run();
        Console.WriteLine($"      Готово, FPS в среднем: {cpuResult.FPSAvg:0} FPS");

        // GPU-тест проводится с включенной трассировкой лучей только если по результату CPU-теста определено, что видеокарта RTX
        var rayTracing = cpuResult.GPUModel.Contains("RTX", StringComparison.OrdinalIgnoreCase);
        gpuProfile = BenchmarkProfile.CreateGpuProfile(nativeWidth, nativeHeight, rayTracing);
        Console.WriteLine("Происходит прогон GPU-теста...");
        settingsWriter.Apply(gpuProfile);
        gpuResult = runner.Run();
        Console.WriteLine($"      Готово, FPS в среднем: {gpuResult.FPSAvg:0} FPS");
    }
    finally
    {
        // В любом случае настройки возвращаются к исходным, закрываем бенчмарк
        runner.StopGame();
        settingsWriter.Restore();
    }

    Console.WriteLine("Формирую отчёт...");
    var report = ReportPrinter.Build(cpuResult, gpuResult, cpuProfile, gpuProfile);

    var reportsDir = Path.Combine(AppContext.BaseDirectory, "reports");
    Directory.CreateDirectory(reportsDir);
    var reportPath = Path.Combine(reportsDir, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    File.WriteAllText(reportPath, report, Encoding.UTF8);

    Console.WriteLine("Готово.");
    Console.WriteLine();
    Console.WriteLine(report);
    Console.WriteLine($"Отчёт сохранён: {reportPath}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"ОШИБКА: {ex.Message}");
    return 1;
}

static string? GetArg(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
