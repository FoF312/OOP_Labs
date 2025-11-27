
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// === Лабораторная 5: система авторизации (всё в одном файле) ===

// --- 1) Тип User (record-подобный, поддерживает сортировку по Name) ---
public record User
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    // Пароль не должен показываться в ToString()
    public string Password { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Address { get; init; }

    public User() { }
    public User(int id, string name, string login, string password, string? email = null, string? address = null)
    {
        Id = id; Name = name; Login = login; Password = password; Email = email; Address = address;
    }

    // Сортировка по полю Name
    public int CompareByName(User? other)
    {
        if (other == null) return 1;
        return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return $"User(Id={Id}, Name={Name}, Login={Login}, Email={Email ?? "<none>"}, Address={Address ?? "<none>"})";
    }
}

// --- 2) Универсальный интерфейс CRUD (Create/Read/Update/Delete) ---
public interface IDataRepository<T>
{
    IEnumerable<T> GetAll();
    T? GetById(int id);
    void Add(T item);
    void Update(T item);
    void Delete(T item);
}

// --- 2b) Интерфейс репозитория пользователей ---
public interface IUserRepository : IDataRepository<User>
{
    User? GetByLogin(string login);
}

// --- 3) Файловый generic-репозиторий на основе JSON ---
public class DataRepository<T> : IDataRepository<T>
{
    private readonly string _filePath;
    private readonly object _locker = new object();
    private List<T> _items = new List<T>();
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public DataRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "data", typeof(T).Name + ".json");
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        lock (_locker)
        {
            if (!File.Exists(_filePath)) { _items = new List<T>(); return; }
            try { _items = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(_filePath), _options) ?? new List<T>(); }
            catch { _items = new List<T>(); }
        }
    }

    private void SaveToFile()
    {
        lock (_locker)
        {
            var json = JsonSerializer.Serialize(_items, _options);
            File.WriteAllText(_filePath, json);
        }
    }

    public IEnumerable<T> GetAll()
    {
        lock (_locker) return _items.ToList();
    }

    public T? GetById(int id)
    {
        lock (_locker)
        {
            foreach (var item in _items)
            {
                var t = item!.GetType();
                var prop = t.GetProperty("Id");
                if (prop != null) {
                    var v = prop.GetValue(item);
                    if (v is int iv && iv == id) return item;
                }
            }
            return default;
        }
    }

    public void Add(T item)
    {
        lock (_locker)
        {
            // если Id == 0 — попробовать присвоить следующий id (если есть свойство/поле Id типа int)
            var idProp = item!.GetType().GetProperty("Id");
            if (idProp != null)
            {
                var current = (int?)idProp.GetValue(item);
                if (current == 0)
                {
                    int next = 1;
                    if (_items.Count > 0)
                    {
                        // найти максимальный существующий id
                        var ids = new List<int>();
                        foreach (var it in _items)
                        {
                            var p = it!.GetType().GetProperty("Id");
                            if (p != null && p.GetValue(it) is int iv) ids.Add(iv);
                        }
                        if (ids.Count > 0) next = ids.Max() + 1;
                    }
                    // попытаться установить Id через reflection (успешно для изменяемых объектов)
                    try { idProp.SetValue(item, next); }
                    catch { /* если объект неизменяемый — ожидается, что вызывающий установит Id */ }
                }
            }

            _items.Add(item);
            SaveToFile();
        }
    }

    public void Update(T item)
    {
        lock (_locker)
        {
            var id = GetIdValue(item);
            if (id == null) return;
            int idx = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                var iv = GetIdValue(_items[i]);
                if (iv != null && iv == id) { idx = i; break; }
            }
            if (idx >= 0) _items[idx] = item;
            SaveToFile();
        }
    }

    public void Delete(T item)
    {
        lock (_locker) { _items.Remove(item); SaveToFile(); }
    }

    private static int? GetIdValue(T? obj)
    {
        if (obj == null) return null;
        var prop = obj.GetType().GetProperty("Id");
        if (prop != null && prop.GetValue(obj) is int iv) return iv;
        return null;
    }
}

// --- 4) UserRepository (реализация репозитория пользователей) ---
public class UserRepository : DataRepository<User>, IUserRepository
{
    public UserRepository(string? filePath = null) : base(filePath) { }

    public User? GetByLogin(string login)
    {
        return GetAll().FirstOrDefault(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
    }
}

// --- 5) Интерфейс сервиса авторизации ---
public interface IAuthService
{
    void SignIn(User user);
    void SignOut();
    bool IsAuthorized { get; }
    User? CurrentUser { get; }
}

// --- 6) Файловая реализация сервиса авторизации с автоматической авторизацией при повторном запуске ---
public class FileAuthService : IAuthService
{
    private readonly string _stateFile;
    private readonly IUserRepository _userRepo;
    private readonly object _lock = new object();

    public User? CurrentUser { get; private set; }

    public bool IsAuthorized => CurrentUser != null;

    public FileAuthService(IUserRepository userRepo, string? stateFile = null)
    {
        _userRepo = userRepo;
        _stateFile = stateFile ?? Path.Combine(AppContext.BaseDirectory, "auth_state.json");
        TryRestore();
    }

    private void TryRestore()
    {
        if (!File.Exists(_stateFile)) return;
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(_stateFile));
            if (doc.RootElement.TryGetProperty("CurrentUserId", out var p) && p.ValueKind == JsonValueKind.Number)
            {
                int id = p.GetInt32();
                var u = _userRepo.GetById(id);
                if (u != null) CurrentUser = u;
            }
        }
        catch { /* игнорируем ошибку и начинаем в неавторизованном состоянии */ }
    }

    private void PersistState()
    {
        lock (_lock)
        {
            var obj = new { CurrentUserId = CurrentUser?.Id };
            File.WriteAllText(_stateFile, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public void SignIn(User user)
    {
        CurrentUser = user;
        PersistState();
    }

    public void SignOut()
    {
        CurrentUser = null;
        PersistState();
    }

    // Вспомогательный метод: проверить логин/пароль и выполнить вход
    public bool Authenticate(string login, string password)
    {
        var u = _userRepo.GetByLogin(login);
        if (u == null) return false;
        if (u.Password == password)
        {
            SignIn(u);
            return true;
        }
        return false;
    }
}

// --- 7) Демонстрация: добавление пользователей, редактирование, вход/выход и автоматическая авторизация ---
static partial class Program
{
    static void DemoAuth()
    {
        Console.WriteLine("\n=== Лабораторная 5: система авторизации (демо) ===\n");

        var usersFile = Path.Combine(AppContext.BaseDirectory, "data_users.json");
        var authFile = Path.Combine(AppContext.BaseDirectory, "auth_state.json");

        // Для демонстрации используем файл репозитория: если он пуст — создаём примеры; существующие данные сохраняются между запусками.
        var userRepo = new UserRepository(usersFile);
        var auth = new FileAuthService(userRepo, authFile);

        Console.WriteLine("1) Убедиться, что в репозитории есть примерные пользователи (Id будет присвоен автоматически, если он отсутствует)");
        var all = userRepo.GetAll().ToList();
        if (!all.Any())
        {
            var u1 = new User(1, "Alice", "alice", "pass123", "alice@example.com", "123 Main St");
            var u2 = new User(2, "Bob", "bob", "s3cret", null, null);
            userRepo.Add(u1);
            userRepo.Add(u2);
            all = userRepo.GetAll().ToList();
        }

        Console.WriteLine("Users (unsorted):");
        foreach (var u in all) Console.WriteLine("  " + u);

        Console.WriteLine("\n2) Cортировать пользователей по Name и вывести:");
        var sorted = all.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var u in sorted) Console.WriteLine("  " + u);

        Console.WriteLine("\n3) Изменить свойство пользователя (обновить email у Bob) и сохранить:");
        var bob = userRepo.GetByLogin("bob");
        if (bob != null)
        {
            var updated = bob with { Email = "bob@nowhere.local" };
            userRepo.Update(updated);
            Console.WriteLine("  Updated: " + userRepo.GetById(updated.Id));
        }

        Console.WriteLine("\n4) Авторизация: вход по логину/паролю");
        bool ok = auth.Authenticate("alice", "pass123");
        Console.WriteLine($"  sign-in alice/pass123 -> success={ok}");
        Console.WriteLine("  Current user: " + (auth.CurrentUser?.ToString() ?? "<none>"));

        Console.WriteLine("\n5) Смена текущего пользователя: выход, попытка входа с неверным паролем и затем с корректным:");
        auth.SignOut();
        Console.WriteLine("  After sign-out, authorized=" + auth.IsAuthorized);
        bool bobWrong = auth.Authenticate("bob", "wrong");
        Console.WriteLine("  bob/wrong -> success=" + bobWrong);
        bool bobOk = auth.Authenticate("bob", "s3cret");
        Console.WriteLine("  bob/s3cret -> success=" + bobOk + ", current=" + (auth.CurrentUser?.Name ?? "<none>"));

        Console.WriteLine("\n6) Автоматическая авторизация между запусками: создаём новый сервис на тех же файлах и показываем восстановленного пользователя:");
        var auth2 = new FileAuthService(userRepo, authFile);
        Console.WriteLine("  New service sees authorized=" + auth2.IsAuthorized + ", current=" + (auth2.CurrentUser?.Name ?? "<none>"));

        Console.WriteLine("\nDemo completed — data written to files in the app directory.\n");
    }

    // ensure main exists - call DemoAuth directly
    static void Main(string[] args)
    {
        // Run the auth demo (keeps sample compact; previous lab demos are intentionally removed)
        DemoAuth();

        Console.WriteLine("Press ENTER to exit...");
        Console.ReadLine();
    }
}
