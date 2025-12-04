using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// ==================== OUTPUT MANAGER ====================
public class OutputManager
{
    private readonly string _filePath;
    private readonly List<string> _buffer = new List<string>();

    public OutputManager(string filePath = "keyboard_output.txt")
    {
        _filePath = filePath;
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    public void WriteLine(string message)
    {
        _buffer.Add(message);
        Console.WriteLine(message);
        File.AppendAllText(_filePath, message + Environment.NewLine);
    }

    // Писал ранее буфер, но так как WriteLine уже пишет в файл - не используется
    public void Flush()
    {
        foreach (var line in _buffer)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
        _buffer.Clear();
    }

    // Удаляет файл логов (очищает предыдущие записи)
    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}

// ==================== COMMAND PATTERN ====================
public interface ICommand
{
    void Execute();
    void Undo();
    string GetDescription();
}

public class PrintCharCommand : ICommand
{
    private readonly OutputManager _output;
    private readonly char _char;

    public PrintCharCommand(OutputManager output, char character)
    {
        _output = output;
        _char = character;
    }

    public void Execute()
    {
        _output.WriteLine(_char.ToString());
    }

    public void Undo()
    {
        _output.WriteLine("[Backspace]");
    }

    // Возвращает описание для сохранения в JSON (не саму команду, а её имя)
    public string GetDescription() => $"Print '{_char}'";
}

public class VolumeUpCommand : ICommand
{
    private readonly OutputManager _output;
    private int _volumeLevel = 20;

    public VolumeUpCommand(OutputManager output, int volumeLevel = 20)
    {
        _output = output;
        _volumeLevel = volumeLevel;
    }

    public void Execute()
    {
        _output.WriteLine($"[Volume Increased +{_volumeLevel}%]");
    }

    public void Undo()
    {
        _output.WriteLine($"[Volume Decreased -{_volumeLevel}%]");
    }

    public string GetDescription() => "Volume Up";
}

public class VolumeDownCommand : ICommand
{
    private readonly OutputManager _output;
    private int _volumeLevel = 20;

    public VolumeDownCommand(OutputManager output, int volumeLevel = 20)
    {
        _output = output;
        _volumeLevel = volumeLevel;
    }

    public void Execute()
    {
        _output.WriteLine($"[Volume Decreased -{_volumeLevel}%]");
    }

    public void Undo()
    {
        _output.WriteLine($"[Volume Increased +{_volumeLevel}%]");
    }

    public string GetDescription() => "Volume Down";
}

public class MediaPlayerCommand : ICommand
{
    private readonly OutputManager _output;

    public MediaPlayerCommand(OutputManager output)
    {
        _output = output;
    }

    public void Execute()
    {
        _output.WriteLine("[Media Player Launched]");
    }

    public void Undo()
    {
        _output.WriteLine("[Media Player Closed]");
    }

    public string GetDescription() => "Media Player";
}

// Главный контроллер - управляет привязками клавиш и историей команд (Undo/Redo)
public class Keyboard
{
    private readonly Dictionary<string, ICommand> _keyBindings = new Dictionary<string, ICommand>();
    private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
    private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
    private readonly OutputManager _output;

    public Keyboard(OutputManager output)
    {
        _output = output;
    }

    public void BindKey(string key, ICommand command)
    {
        _keyBindings[key] = command;
    }

    // Важно: очищаем redo при выполнении новой команды (стандартное поведение Undo/Redo)
    public void PressKey(string key)
    {
        if (_keyBindings.ContainsKey(key))
        {
            ICommand command = _keyBindings[key];
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        }
        else
        {
            _output.WriteLine($"[Key '{key}' is not bound]");
        }
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            ICommand command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }
        else
        {
            _output.WriteLine("[No command to undo]");
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            ICommand command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }
        else
        {
            _output.WriteLine("[No command to redo]");
        }
    }

    public Dictionary<string, ICommand> GetKeyBindings()
    {
        return new Dictionary<string, ICommand>(_keyBindings);
    }

    public void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}

// ==================== SERIALIZATION FRAMEWORK ====================

public interface ISerializable
{
    Dictionary<string, object> ToDictionary();
}

public class SerializationSchema
{
    public class PropertyMapping
    {
        public string SourcePropertyName { get; set; } = string.Empty;
        public string TargetPropertyName { get; set; } = string.Empty;
        public bool Include { get; set; } = true;
        public Func<object, object>? Transformer { get; set; }
    }

    public string TypeName { get; set; } = string.Empty;
    public List<PropertyMapping> PropertyMappings { get; set; } = new List<PropertyMapping>();

    public PropertyMapping Map(string sourceProperty, string? targetProperty = null, bool include = true)
    {
        var mapping = new PropertyMapping
        {
            SourcePropertyName = sourceProperty,
            TargetPropertyName = targetProperty ?? sourceProperty,
            Include = include
        };
        PropertyMappings.Add(mapping);
        return mapping;
    }
}

public class JsonSerializer
{
    private readonly Dictionary<string, SerializationSchema> _schemas = new Dictionary<string, SerializationSchema>();

    // Регистрирует схему - определяет правила преобразования объекта в JSON
    public void RegisterSchema(string typeName, SerializationSchema schema)
    {
        schema.TypeName = typeName;
        _schemas[typeName] = schema;
    }

    // Оборачивает данные с типом и преобразует в JSON
    public string Serialize(string typeName, Dictionary<string, object> data)
    {
        var wrapper = new { type = typeName, data = data };
        return System.Text.Json.JsonSerializer.Serialize(wrapper);
    }

    // Сериализует коллекцию с именем контейнера и массивом элементов
    public string SerializeCollection(string collectionName, List<Dictionary<string, object>> items)
    {
        var wrapper = new { name = collectionName, items = items };
        return System.Text.Json.JsonSerializer.Serialize(wrapper);
    }

    // Применяет зарегистрированную схему: переименует поля и применяет трансформации
    public Dictionary<string, object> ApplySchema(string typeName, Dictionary<string, object> rawData)
    {
        if (!_schemas.ContainsKey(typeName))
            return rawData;

        var schema = _schemas[typeName];
        var result = new Dictionary<string, object>();

        foreach (var mapping in schema.PropertyMappings.Where(m => m.Include))
        {
            if (rawData.ContainsKey(mapping.SourcePropertyName))
            {
                var value = rawData[mapping.SourcePropertyName];
                if (mapping.Transformer != null)
                    value = mapping.Transformer(value);

                result[mapping.TargetPropertyName] = value;
            }
        }

        return result;
    }
}

public class JsonDeserializer
{
    public Dictionary<string, object> Deserialize(string json)
    {
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                return JsonElementToDictionary(doc.RootElement);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Deserialization Error: {ex.Message}]");
            return new Dictionary<string, object>();
        }
    }

    // Рекурсивно преобразует JsonElement в словарь (обрабатывает вложенные объекты)
    private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var value = JsonElementToObject(property.Value);
                if (value != null)
                    dict[property.Name] = value;
            }
        }

        return dict;
    }

    // Преобразует любой тип JSON значения в C# объект (примитивы, массивы, объекты)
    private object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => JsonElementToDictionary(element),
            _ => element.ToString()
        };
    }
}

// ==================== MEMENTO PATTERN ====================

public class KeyboardMemento
{
    public Dictionary<string, string> KeyBindings { get; set; } = new Dictionary<string, string>();
    public DateTime SaveTime { get; set; } = DateTime.Now;
}

// Caretaker для сохранения/загрузки состояния клавиатуры в JSON
public class KeyboardStateCaretaker
{
    private readonly string _filePath;
    private readonly JsonSerializer _serializer;
    private readonly JsonDeserializer _deserializer;
    private readonly SerializationSchema _schema;

    public KeyboardStateCaretaker(string filePath = "keyboard_bindings.json")
    {
        _filePath = filePath;
        _serializer = new JsonSerializer();
        _deserializer = new JsonDeserializer();

        _schema = new SerializationSchema();
        _schema.Map("key");
        _schema.Map("command");
        _serializer.RegisterSchema("KeyBinding", _schema);
    }

    // Важно: сохраняем привязки в файл для последующей загрузки
    public void SaveState(Keyboard keyboard)
    {
        var bindings = keyboard.GetKeyBindings();
        var bindingsList = new List<Dictionary<string, object>>();

        foreach (var kvp in bindings)
        {
            bindingsList.Add(new Dictionary<string, object>
            {
                { "key", kvp.Key },
                { "command", kvp.Value.GetDescription() }
            });
        }

        string json = _serializer.SerializeCollection("KeyboardBindings", bindingsList);
        File.WriteAllText(_filePath, json);
    }

    public List<Dictionary<string, object>> LoadState()
    {
        if (!File.Exists(_filePath))
            return new List<Dictionary<string, object>>();

        try
        {
            string json = File.ReadAllText(_filePath);
            var deserializer = new JsonDeserializer();
            var data = deserializer.Deserialize(json);

            if (data.ContainsKey("items") && data["items"] is List<object> items)
            {
                return items.Cast<Dictionary<string, object>>().ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error loading state: {ex.Message}]");
        }

        return new List<Dictionary<string, object>>();
    }
}

// ==================== MAIN PROGRAM ====================
class Program
{
    static void Main()
    {
        var output = new OutputManager("keyboard_output.txt");
        var keyboard = new Keyboard(output);
        var caretaker = new KeyboardStateCaretaker("keyboard_bindings.json");

        output.WriteLine("=== Virtual Keyboard Lab 6 ===");
        output.WriteLine("");

        // Создаём команды
        var printA = new PrintCharCommand(output, 'a');
        var printB = new PrintCharCommand(output, 'b');
        var printC = new PrintCharCommand(output, 'c');
        var printD = new PrintCharCommand(output, 'd');
        var volumeUp = new VolumeUpCommand(output, 20);
        var volumeDown = new VolumeDownCommand(output, 20);
        var mediaPlayer = new MediaPlayerCommand(output);

        // Привязываем клавиши к командам
        keyboard.BindKey("a", printA);
        keyboard.BindKey("b", printB);
        keyboard.BindKey("c", printC);
        keyboard.BindKey("d", printD);
        keyboard.BindKey("ctrl++", volumeUp);
        keyboard.BindKey("ctrl+-", volumeDown);
        keyboard.BindKey("ctrl+p", mediaPlayer);

        // Сохраняем состояние привязок
        caretaker.SaveState(keyboard);

        // Демонстрация работы
        output.WriteLine("--- Printing Characters ---");
        keyboard.PressKey("a");
        keyboard.PressKey("b");
        keyboard.PressKey("c");

        output.WriteLine("");
        output.WriteLine("--- Testing Undo ---");
        keyboard.Undo();
        keyboard.Undo();

        output.WriteLine("");
        output.WriteLine("--- Testing Redo ---");
        keyboard.Redo();

        output.WriteLine("");
        output.WriteLine("--- Volume Control ---");
        keyboard.PressKey("ctrl++");
        keyboard.PressKey("ctrl+-");

        output.WriteLine("");
        output.WriteLine("--- Media Player ---");
        keyboard.PressKey("ctrl+p");

        output.WriteLine("");
        output.WriteLine("--- More Characters ---");
        keyboard.PressKey("d");

        output.WriteLine("");
        output.WriteLine("--- Undo Actions ---");
        keyboard.Undo();
        keyboard.Undo();

        output.WriteLine("");
        output.WriteLine("=== Loaded Bindings from File ===");
        var loadedBindings = caretaker.LoadState();
        foreach (var binding in loadedBindings)
        {
            output.WriteLine($"Key: {binding["key"]} -> Command: {binding["command"]}");
        }

        output.WriteLine("");
        output.WriteLine("--- Demonstration Complete ---");
    }
}
