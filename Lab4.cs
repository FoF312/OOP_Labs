
// --- Лабораторная работа 4: события / валидация ---

// Интерфейс обработчика событий (generic)
public interface IEventHandler<TEventArgs>
{
    void Handle(object sender, TEventArgs args);
}

// Класс Event<TEventArgs> — хранит подписчиков и умеет оповещать их.
// Поддерживаются операции += и -= (через перегрузку операторов + и -).
public class Event<TEventArgs>
{
    private readonly List<IEventHandler<TEventArgs>> _handlers = new();

    // добавление подписчика (можно использовать instance += handler)
    public static Event<TEventArgs> operator +(Event<TEventArgs> ev, IEventHandler<TEventArgs> handler)
    {
        if (ev == null) throw new ArgumentNullException(nameof(ev));
        if (handler != null) ev._handlers.Add(handler);
        return ev;
    }

    // удаление подписчика (можно использовать instance -= handler)
    public static Event<TEventArgs> operator -(Event<TEventArgs> ev, IEventHandler<TEventArgs> handler)
    {
        if (ev == null) throw new ArgumentNullException(nameof(ev));
        if (handler != null) ev._handlers.Remove(handler);
        return ev;
    }

    // вызов события — оповестить всех подписчиков (в порядке добавления)
    public void Invoke(object sender, TEventArgs args)
    {
        var snapshot = _handlers.ToArray();
        foreach (var h in snapshot)
        {
            try { h.Handle(sender, args); }
            catch { /* подписчики не должны рвать цепочку */ }
        }
    }

    // Явные методы для управления подписками (альтернатива +=/-=)
    public void Add(IEventHandler<TEventArgs> handler) => _handlers.Add(handler);
    public void Remove(IEventHandler<TEventArgs> handler) => _handlers.Remove(handler);
}

// EventArgs-подобные классы
public class PropertyChangedEventArgs
{
    public string PropertyName { get; }
    public PropertyChangedEventArgs(string propertyName) => PropertyName = propertyName;
}

public class PropertyChangingEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    // Флаг, разрешающий/отменяющий изменение (валидатор может выставить false)
    public bool CanChange { get; set; } = true;

    public PropertyChangingEventArgs(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

// Примеры обработчиков: печать изменений после/до и валидатор до изменения

// Выводит в консоль уведомления о изменённом свойстве (PropertyChanged)
public class ConsolePropertyChangedHandler : IEventHandler<PropertyChangedEventArgs>
{
    public void Handle(object sender, PropertyChangedEventArgs args)
    {
        Console.WriteLine($"[CHANGED] {sender.GetType().Name}.{args.PropertyName} изменено.");
    }
}

// Выводит в консоль уведомления о начале изменения (PropertyChanging)
public class ConsolePropertyChangingHandler : IEventHandler<PropertyChangingEventArgs>
{
    public void Handle(object sender, PropertyChangingEventArgs args)
    {
        Console.WriteLine($"[CHANGING] {sender.GetType().Name}.{args.PropertyName}: {args.OldValue} -> {args.NewValue}");
    }
}

// Валидатор для свойств (можно настраивать правила). Возвращает CanChange=false при нарушениях.
public class PropertyChangeValidator : IEventHandler<PropertyChangingEventArgs>
{
    public void Handle(object sender, PropertyChangingEventArgs args)
    {
        var name = args.PropertyName;
        var newVal = args.NewValue;

        // Age: целое >= 0
        if (name.IndexOf("Age", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (newVal == null || !int.TryParse(newVal.ToString(), out int v) || v < 0)
            {
                args.CanChange = false;
                Console.WriteLine($"[VALIDATOR] Отклонено: {name} должно быть неотрицательным целым.");
                return;
            }
        }

        // Email: должно содержать '@'
        if (name.IndexOf("Email", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (newVal == null || !newVal.ToString()!.Contains('@'))
            {
                args.CanChange = false;
                Console.WriteLine($"[VALIDATOR] Отклонено: {name} должен содержать '@'.");
                return;
            }
        }

        // Temperature: допустимый диапазон (-100..+200)
        if (name.IndexOf("Temperature", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (newVal == null || !double.TryParse(newVal.ToString(), out double t) || t < -100 || t > 200)
            {
                args.CanChange = false;
                Console.WriteLine($"[VALIDATOR] Отклонено: {name} вне диапазона [-100;200].");
                return;
            }
        }

        // По умолчанию — разрешаем изменение
    }
}

/*
  Примеры классов с минимум 3 полями, которые посылают событие PropertyChanging перед изменением,
  и PropertyChanged после успешного изменения.
*/

// Пример 1: профиль пользователя
public class UserProfile
{
    // События — поля, чтобы "+=" / "-=" работали на экземпляре
    public Event<PropertyChangingEventArgs> PropertyChanging = new Event<PropertyChangingEventArgs>();
    public Event<PropertyChangedEventArgs> PropertyChanged = new Event<PropertyChangedEventArgs>();

    private string _username = "";
    private string _email = "";
    private int _age = 0;

    public string Username
    {
        get => _username;
        set => SetProperty(nameof(Username), _username, value,
            v => _username = (string)v!);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(nameof(Email), _email, value,
            v => _email = (string)v!);
    }

    public int Age
    {
        get => _age;
        set => SetProperty(nameof(Age), _age, value,
            v => _age = (int)v!);
    }

    // Общая логика установки свойства с событиями
    private void SetProperty(string propertyName, object? oldValue, object? newValue, Action<object?> setter)
    {
        var before = new PropertyChangingEventArgs(propertyName, oldValue, newValue);
        PropertyChanging.Invoke(this, before);

        if (!before.CanChange)
        {
            Console.WriteLine($"[INFO] Изменение {propertyName} отменено валидатором.");
            return;
        }

        setter(newValue);

        var after = new PropertyChangedEventArgs(propertyName);
        PropertyChanged.Invoke(this, after);
    }
}

// Пример 2: сенсорное устройство с тремя измерениями
public class SensorDevice
{
    // События — поля, чтобы "+=" / "-=" работали на экземпляре
    public Event<PropertyChangingEventArgs> PropertyChanging = new Event<PropertyChangingEventArgs>();
    public Event<PropertyChangedEventArgs> PropertyChanged = new Event<PropertyChangedEventArgs>();

    private double _temperature;
    private double _pressure;
    private string _status = "OK";

    public double Temperature
    {
        get => _temperature;
        set => SetProperty(nameof(Temperature), _temperature, value, v => _temperature = (double)v!);
    }

    public double Pressure
    {
        get => _pressure;
        set => SetProperty(nameof(Pressure), _pressure, value, v => _pressure = (double)v!);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(nameof(Status), _status, value, v => _status = (string)v!);
    }

    private void SetProperty(string propertyName, object? oldValue, object? newValue, Action<object?> setter)
    {
        var before = new PropertyChangingEventArgs(propertyName, oldValue, newValue);
        PropertyChanging.Invoke(this, before);

        if (!before.CanChange)
        {
            Console.WriteLine($"[INFO] Изменение {propertyName} отменено валидатором.");
            return;
        }

        setter(newValue);
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Демонстрация Lab4 — выводит процесс валидации, отмен и успешных изменений
static partial class Program // partial чтобы не мешать основному классу (если он уже объявлен)
{
    static void DemoEvents()
    {
        Console.WriteLine("\n=== Лабораторная 4: события и валидация (демо) ===\n");

        // Создаём обработчики
        var changedHandler = new ConsolePropertyChangedHandler();
        var changingHandler = new ConsolePropertyChangingHandler();
        var validator = new PropertyChangeValidator();

        // UserProfile demo
        var user = new UserProfile();
        // Подписываемся: сначала валидатор (before), затем печать before и after
        user.PropertyChanging += validator;
        user.PropertyChanging += changingHandler;
        user.PropertyChanged += changedHandler;

        Console.WriteLine("UserProfile: пытаемся установить корректные значения:");
        user.Username = "alice";
        user.Email = "alice@example.com";
        user.Age = 30;

        Console.WriteLine("\nUserProfile: пытаемся установить некорректные значения:");
        user.Age = -5;                     // валидатор должен запретить
        user.Email = "invalid-email";      // валидатор должен запретить

        Console.WriteLine("\n---\nSensorDevice demo (температура/давление/status):");
        var sensor = new SensorDevice();
        sensor.PropertyChanging += validator;     // одинаковый валидатор применим к Temperature
        sensor.PropertyChanged += changedHandler;
        sensor.PropertyChanging += changingHandler;

        Console.WriteLine("Установка допустимой температуры (25.5):");
        sensor.Temperature = 25.5;

        Console.WriteLine("Попытка установить недопустимой температуры (1000):");
        sensor.Temperature = 1000; // должен запретить

        Console.WriteLine("Установка давления и статуса:");
        sensor.Pressure = 1.2;
        sensor.Status = "ALARM";

        Console.WriteLine("\n=== Демонстрация Lab4 завершена ===\n");
    }
}

static partial class Program
{
    static void Main(string[] args)
    {
        // call the demo from this simple launcher
        DemoEvents();

        // keep console open in interactive runs
        Console.WriteLine("Press ENTER to exit...");
        Console.ReadLine();
    }
}

// Подключаем вызов DemoEvents() в основной DemoAll, если он там есть
// Если нужно — добавлю вызов DemoEvents() в DemoAll() в другом месте файла.