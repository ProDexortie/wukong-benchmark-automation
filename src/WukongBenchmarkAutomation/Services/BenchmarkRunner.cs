using System.Diagnostics;
using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services.Contracts;

namespace WukongBenchmarkAutomation.Services;

// Запускает игру через Steam, ждёт файл результата, закрывает игру и читает результат
public class BenchmarkRunner : IBenchmarkRunner
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RunTimeout = TimeSpan.FromMinutes(15);

    private readonly string _steamExe;
    private readonly string _gameDir;
    private readonly string _resultsDir;

    public BenchmarkRunner(string steamExe, string gameDir)
    {
        _steamExe = steamExe;
        _gameDir = gameDir;
        // Результаты бенчмарка сохраняются в %TEMP%
        _resultsDir = Path.Combine(Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool");
    }

    public bool IsGameRunning() => FindGameProcesses().Count > 0;

    public BenchmarkResult Run()
    {
        Directory.CreateDirectory(_resultsDir);
        var knownFiles = Directory.GetFiles(_resultsDir).Select(Path.GetFileName).ToHashSet();

        // Запуск через Steam, аргумент -benchmark сразу запускает бенчмарк, минуя меню
        Process.Start(new ProcessStartInfo(_steamExe, $"-applaunch {SteamLocator.AppId} -benchmark")
        {
            UseShellExecute = false,
        });

        var timer = Stopwatch.StartNew();
        var gameSeen = false;
        var lastSeenRunning = DateTime.UtcNow;

        while (timer.Elapsed < RunTimeout)
        {
            Thread.Sleep(PollInterval);

            var newFile = Directory.GetFiles(_resultsDir).FirstOrDefault(f => !knownFiles.Contains(Path.GetFileName(f)));
            if (newFile != null)
            {
                var result = TryReadResult(newFile);
                if (result != null)
                {
                    StopGame();
                    return result;
                }
                continue; // файл появился, но ещё дописывается, нужно подождать
            }

            // Если бенчмарк запустился, а потом закрылся без результата, дальше ждать нет смысла
            if (IsGameRunning())
            {
                gameSeen = true;
                lastSeenRunning = DateTime.UtcNow;
            }
            else if (gameSeen && DateTime.UtcNow - lastSeenRunning > TimeSpan.FromSeconds(15))
            {
                throw new InvalidOperationException("Бенчмарк закрылся, не создав файл результата.");
            }
        }

        StopGame();
        throw new TimeoutException($"Файл результата не появился за {RunTimeout.TotalMinutes:0} минут.");
    }

    // Файл может записаться не сразу - пока он не дописан, JSON не разбирается
    private static BenchmarkResult? TryReadResult(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return BenchmarkResult.Parse(reader.ReadToEnd());
        }
        catch (IOException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // Закрывает все процессы бенчмарка и ждёт, пока они завершатся
    public void StopGame()
    {
        foreach (var process in FindGameProcesses())
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(15000);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // процесс уже завершился сам
            }
        }

        // Небольшая пауза, чтобы Steam прогрузился и увидел, что игра закрыта
        Thread.Sleep(5000);
    }

    // Запущенный бенчмарк состоит из процессов лаунчера b1_benchmark.exe и самого бенча b1-Win64-Shipping.exe, поэтому поиск проводится по папке, из которой они запущены, а не по имени
    private List<Process> FindGameProcesses()
    {
        var found = new List<Process>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path != null && path.StartsWith(_gameDir, StringComparison.OrdinalIgnoreCase))
                    found.Add(process);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // у системных процессов нет доступа к MainModule
            }
        }
        return found;
    }
}
