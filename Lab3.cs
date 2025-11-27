using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

// --- Логические типы и интерфейсы (описание контрактов системы) ---

// Уровни логов
public enum LogLevel
{
    INFO = 0,
    WARN = 1,
    ERROR = 2
}

// Интерфейс фильтра — решает, пропускать ли сообщение
public interface ILogFilter
{
    bool Match(LogLevel level, string text);
}

// Интерфейс форматтера — преобразует текст лога
public interface ILogFormatter
{
    string Format(LogLevel level, string text);
}

// Интерфейс обработчика — принимает уже отформатированный текст и выводит/пересылает его
public interface ILogHandler : IDisposable
{
    void Handle(LogLevel level, string text);
}

// --- Реализации фильтров ---

// Простой фильтр по подстроке (по умолчанию регистронезависимый)
public class SimpleLogFilter : ILogFilter
{
    private readonly string _pattern;
    private readonly StringComparison _cmp;

    public SimpleLogFilter(string pattern, bool caseSensitive = false)
    {
        _pattern = pattern ?? "";
        _cmp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }

    // Возвращает true если pattern содержится в text
    public bool Match(LogLevel level, string text)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOf(_pattern, _cmp) >= 0;
    }
}

// Фильтр на основе регулярного выражения
public class ReLogFilter : ILogFilter
{
    private readonly Regex _regex;

    public ReLogFilter(string pattern, RegexOptions opts = RegexOptions.None)
    {
        _regex = new Regex(pattern ?? string.Empty, opts);
    }

    public bool Match(LogLevel level, string text)
    {
        return _regex.IsMatch(text ?? "");
    }
}

// Фильтр по уровню: можно проверять точный уровень или "минимальный" с allowHigher=true
public class LevelFilter : ILogFilter
{
    private readonly LogLevel _minLevel;
    private readonly bool _allowHigher;

    public LevelFilter(LogLevel minLevel, bool allowHigher = false)
    {
        _minLevel = minLevel;
        _allowHigher = allowHigher;
    }

    // Если allowHigher == true — пропускает сообщения уровня >= minLevel
    // иначе — только точный уровень (==)
    public bool Match(LogLevel level, string text)
    {
        return _allowHigher ? level >= _minLevel : level == _minLevel;
    }
}

// --- Форматтеры ---

// Базовый форматтер, добавляет уровень и метку времени к тексту
public class BasicFormatter : ILogFormatter
{
    // Формат вывода: [LEVEL] [yyyy.MM.dd HH:mm:ss] message
    public string Format(LogLevel level, string text)
    {
        string now = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
        return $"[{level}] [{now}] {text}";
    }
}

// --- Обработчики (Handlers) ---

// Обработчик вывода в консоль с цветами — потокобезопасный
public class ConsoleHandler : ILogHandler
{
    private static readonly object _lock = new();

    public void Handle(LogLevel level, string text)
    {
        lock (_lock)
        {
            var orig = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.INFO => ConsoleColor.White,
                LogLevel.WARN => ConsoleColor.Yellow,
                LogLevel.ERROR => ConsoleColor.Red,
                _ => orig
            };
            Console.WriteLine(text);
            Console.ForegroundColor = orig;
        }
    }

    // Ничего не освобождаем — реализация для совместимости с using
    public void Dispose() { }
}

// Обработчик записи в файл — защищён потоковым доступом и использует AutoFlush для надежности
public class FileHandler : ILogHandler
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public FileHandler(string path, bool append = true, Encoding? enc = null)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Создаём поток с возможностью чтения параллельно для удобства дебага
        FileStream fs = new(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(fs, enc ?? Encoding.UTF8) { AutoFlush = true };
    }

    // Записываем одну строку — ошибки подавляем, чтобы логирование не ломало приложение
    public void Handle(LogLevel level, string text)
    {
        try
        {
            lock (_lock) _writer.WriteLine(text);
        }
        catch { }
    }

    public void Dispose() => _writer.Dispose();
}

// Обработчик отправки через UDP — простой пример сетевого отправителя.
// В демо ошибки сети подавляются.
public class SocketHandler : ILogHandler
{
    private readonly UdpClient _udp;
    private readonly IPEndPoint _remote;

    public SocketHandler(string host, int port)
    {
        _udp = new UdpClient();
        var addr = Dns.GetHostAddresses(host).FirstOrDefault() ?? IPAddress.Loopback;
        _remote = new IPEndPoint(addr, port);
    }

    public void Handle(LogLevel level, string text)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            _udp.Send(data, data.Length, _remote);
        }
        catch { }
    }

    public void Dispose() => _udp.Dispose();
}

// Простой эмулятор системного журнала — в реальном приложении здесь можно вызывать системный API
public class SyslogHandler : ILogHandler
{
    public void Handle(LogLevel level, string text)
    {
        // Делаем пометку чтобы видно было, что это системный лог (эмуляция)
        Console.WriteLine($"[SYSLOG] {text}");
    }

    public void Dispose() { }
}

// Эмуляция FTP-обработчика — вместо реального FTP пишем в локальный файл.
// Это даёт стабильную демонстрацию без внешних зависимостей.
public class FtpHandler : ILogHandler
{
    private readonly string _emulationLocalFile;

    public FtpHandler(string remoteHost, string user, string pass)
    {
        var safeHost = new string((remoteHost ?? "ftp").Where(char.IsLetterOrDigit).ToArray());
        _emulationLocalFile = Path.Combine(Environment.CurrentDirectory, $"ftp_{safeHost}_upload.log");
    }

    // Добавлено: доступ к файлу эмулятора для демонстрации/тестов
    public string EmulationLocalFile => _emulationLocalFile;

    public void Handle(LogLevel level, string text)
    {
        try
        {
            File.AppendAllText(_emulationLocalFile, text + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    public void Dispose() { }
}

// --- Класс Logger — композиция фильтров, форматтеров и обработчиков ---

public class Logger
{
    private readonly ILogFilter[] _filters;
    private readonly ILogFormatter[] _formatters;
    private readonly ILogHandler[] _handlers;

    // Конструктор принимает наборы компонентов (можно передать null — используются пустые массивы)
    public Logger(IEnumerable<ILogFilter>? filters = null,
                  IEnumerable<ILogFormatter>? formatters = null,
                  IEnumerable<ILogHandler>? handlers = null)
    {
        _filters = filters?.ToArray() ?? Array.Empty<ILogFilter>();
        _formatters = formatters?.ToArray() ?? Array.Empty<ILogFormatter>();
        _handlers = handlers?.ToArray() ?? Array.Empty<ILogHandler>();
    }

    // Главный метод: проверяет фильтры, форматирует сообщение последовательно всеми форматтерами
    // и передаёт полученный текст всем обработчикам
    public void Log(LogLevel level, string text)
    {
        if (!PassesFilters(level, text)) return;

        string current = text ?? "";
        foreach (var f in _formatters)
        {
            try { current = f.Format(level, current) ?? ""; }
            catch { /* форматтер упал — продолжаем с текущим значением */ }
        }

        foreach (var h in _handlers)
        {
            try { h.Handle(level, current); }
            catch { /* обработчики не должны ломать приложение — ошибки подавляем */ }
        }
    }

    // Все фильтры применяются как логическая И (AND).
    private bool PassesFilters(LogLevel level, string text)
    {
        if (_filters.Length == 0) return true;
        foreach (var f in _filters)
        {
            try
            {
                if (!f.Match(level, text)) return false;
            }
            catch
            {
                // Если фильтр упал — считаем, что он не пропускает
                return false;
            }
        }
        return true;
    }

    // Удобные методы для конкретных уровней
    public void LogInfo(string text) => Log(LogLevel.INFO, text);
    public void LogWarn(string text) => Log(LogLevel.WARN, text);
    public void LogError(string text) => Log(LogLevel.ERROR, text);
}

// --- Точка входа и демонстрация работы логгера ---

class Program
{
    static void Main()
    {
        DemoAll();
    }

    // Показывает пример использования всех компонентов: фильтров, форматтеров, обработчиков и логгеров.
    static void DemoAll()
    {
        Console.WriteLine("=== Лабораторная работа 3: Система логирования (демо) ===\n");

        // единый форматтер для всех логгеров
        var basicFormatter = new BasicFormatter();

        // используемые обработчики (using обеспечивает Dispose у ресурсов)
        using var fileHandler = new FileHandler("app_logs.txt");
        using var socketHandler = new SocketHandler("127.0.0.1", 514);
        using var consoleHandler = new ConsoleHandler();
        using var syslogHandler = new SyslogHandler();
        using var ftpHandler = new FtpHandler("ftp.example.com", "user", "pass");

        // создаём фильтры: WARN+ (minLevel с allowHigher), простой поиск и регекс
        var warnPlusFilter = new LevelFilter(LogLevel.WARN, allowHigher: true);
        var containsDisk = new SimpleLogFilter("disk");
        var reAuth = new ReLogFilter(@"auth|login", RegexOptions.IgnoreCase);

        // logger1: перехватывает WARN и ERROR, пишет в консоль и файл
        var logger1 = new Logger(
            filters: new ILogFilter[] { warnPlusFilter },
            formatters: new ILogFormatter[] { basicFormatter },
            handlers: new ILogHandler[] { consoleHandler, fileHandler }
        );

        // logger2: ловит сообщения, подходящие под regex "auth|login", отправляет в консоль, UDP, syslog и эмулирует FTP
        var logger2 = new Logger(
            filters: new ILogFilter[] { reAuth },
            formatters: new ILogFormatter[] { basicFormatter },
            handlers: new ILogHandler[] { consoleHandler, socketHandler, syslogHandler, ftpHandler }
        );

        // logger3: комбинированный фильтр — содержит "disk" И уровень WARN+
        var logger3 = new Logger(
            filters: new ILogFilter[] { containsDisk, warnPlusFilter },
            formatters: new ILogFormatter[] { basicFormatter },
            handlers: new ILogHandler[] { consoleHandler }
        );

        Console.WriteLine("-- Пример 1: logger1 (WARN+) -> консоль + файл --");
        logger1.LogInfo("Info: Application started");
        logger1.LogWarn("Warn: Low disk space on /dev/sda1");
        logger1.LogError("Error: Disk read error on /dev/sda1");

        Console.WriteLine("\n-- Пример 2: logger2 (регекс auth/login) -> консоль + UDP + SYSLOG + FTP (эмуляция) --");
        logger2.LogInfo("User login succeeded");      // совпало "login" -> пройдет
        logger2.LogInfo("Session started for user");  // не пройдет
        logger2.LogWarn("Auth attempt failed for user"); // совпало -> пройдет
        logger2.LogError("Multiple login attempts (auth)"); // совпало -> пройдет

        Console.WriteLine("\n-- Пример 3: logger3 (contains 'disk' && WARN+) -> только сообщения с 'disk' и WARN/ERROR --");
        logger3.LogWarn("CPU temperature high"); // не содержит 'disk' -> отфильтровано
        logger3.LogWarn("Disk usage at 95%");   // содержит 'disk' и WARN -> пройдёт

        Console.WriteLine("\n-- Дополнительные примеры --");
        var allLogger = new Logger(null, new ILogFormatter[] { basicFormatter }, new ILogHandler[] { consoleHandler });
        allLogger.LogInfo("Generic info message");
        allLogger.LogError("Critical error for investigation");

        Console.WriteLine("\n=== Демонстрация завершена. Проверьте файлы для записей: ===");

        // Печатаем абсолютные пути лог-файла и файла FTP-эмулятора (чтобы не искать вручную)
        Console.WriteLine($"  app_logs: {Path.GetFullPath("app_logs.txt")}");
        Console.WriteLine($"  ftp emulation: {Path.GetFullPath(ftpHandler.EmulationLocalFile)}");
    }
}