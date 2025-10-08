using System;
using System.Collections.Generic;

public class Angle
{
    private double radians;
    private const double TwoPi = 2 * Math.PI;

    // Создает новый угол из радиан или градусов
    public Angle(double value = 0, bool isRadians = true)
    {
        if (isRadians)
            radians = Normalize(value);
        else
            radians = Normalize(DegreesToRadians(value));
    }

    // Угол в радианах (автоматически нормализуется)
    public double Radians
    {
        get => radians;
        set => radians = Normalize(value);
    }

    // Угол в градусах (автоматически конвертируется и нормализуется)
    public double Degrees
    {
        get => RadiansToDegrees(radians);
        set => radians = Normalize(DegreesToRadians(value));
    }

    // Нормализует угол в диапазон [0, 2π)
    private double Normalize(double rad)
    {
        double result = rad % TwoPi;
        if (result < 0)
            result += TwoPi;
        return result;
    }

    private double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
    private double RadiansToDegrees(double rad) => rad * 180.0 / Math.PI;

    // Преобразование в другие типы
    public float ToFloat() => (float)radians;
    public int ToInt() => (int)Math.Round(radians);
    
    // Строковые представления
    public override string ToString() => $"{Degrees:0.0}°";
    public string ToString(string format) => format.ToLower() switch
    {
        "rad" or "radians" => $"{radians:0.00} rad",
        "deg" or "degrees" => $"{Degrees:0.0}°",
        _ => ToString()
    };

    // Реализация repr (более детальное представление)
    public string ToReprString() => $"Angle(radians={radians:0.0000}, degrees={Degrees:0.00})";

    // Проверяет равенство углов с учетом нормализации
    public override bool Equals(object? obj) => obj is Angle other && Math.Abs(radians - other.radians) < 0.0001;
    public override int GetHashCode() => radians.GetHashCode();

    // Операторы сравнения
    public static bool operator ==(Angle? a, Angle? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));
    public static bool operator !=(Angle? a, Angle? b) => !(a == b);
    public static bool operator <(Angle a, Angle b) => a.radians < b.radians;
    public static bool operator >(Angle a, Angle b) => a.radians > b.radians;
    public static bool operator <=(Angle a, Angle b) => a.radians <= b.radians;
    public static bool operator >=(Angle a, Angle b) => a.radians >= b.radians;

    // Арифметические операторы
    public static Angle operator +(Angle a, Angle b) => new Angle(a.radians + b.radians);
    public static Angle operator +(Angle a, double b) => new Angle(a.radians + b);
    public static Angle operator +(Angle a, int b) => new Angle(a.radians + b);
    public static Angle operator +(double a, Angle b) => new Angle(a + b.radians);
    public static Angle operator +(int a, Angle b) => new Angle(a + b.radians);
    
    public static Angle operator -(Angle a, Angle b) => new Angle(a.radians - b.radians);
    public static Angle operator -(Angle a, double b) => new Angle(a.radians - b);
    public static Angle operator -(Angle a, int b) => new Angle(a.radians - b);
    
    public static Angle operator *(Angle a, double num) => new Angle(a.radians * num);
    public static Angle operator *(Angle a, int num) => new Angle(a.radians * num);
    public static Angle operator *(double num, Angle a) => new Angle(a.radians * num);
    public static Angle operator *(int num, Angle a) => new Angle(a.radians * num);
    
    public static Angle operator /(Angle a, double num) => new Angle(a.radians / num);
    public static Angle operator /(Angle a, int num) => new Angle(a.radians / num);

    // Явные преобразования
    public static explicit operator float(Angle angle) => angle.ToFloat();
    public static explicit operator int(Angle angle) => angle.ToInt();
    public static explicit operator double(Angle angle) => angle.radians;
}

public class AngleRange
{
    public Angle Start { get; }
    public Angle End { get; }
    public bool IncludeStart { get; }
    public bool IncludeEnd { get; }

    // Конструкторы
    public AngleRange(Angle start, Angle end, bool includeStart = true, bool includeEnd = true)
    {
        Start = start;
        End = end;
        IncludeStart = includeStart;
        IncludeEnd = includeEnd;
    }

    public AngleRange(double start, double end, bool isRadians = true, 
                     bool includeStart = true, bool includeEnd = true)
        : this(new Angle(start, isRadians), new Angle(end, isRadians), includeStart, includeEnd)
    {
    }

    // Длина промежутка (через свойство и метод)
    public double Length => GetLength();
    public double GetLength()
    {
        if (Start.Radians <= End.Radians)
            return End.Radians - Start.Radians;
        else
            return (2 * Math.PI - Start.Radians) + End.Radians;
    }

    // Проверка принадлежности
    public bool Contains(Angle angle)
    {
        double angleRad = angle.Radians;
        double startRad = Start.Radians;
        double endRad = End.Radians;

        if (startRad <= endRad)
        {
            bool afterStart = IncludeStart ? angleRad >= startRad : angleRad > startRad;
            bool beforeEnd = IncludeEnd ? angleRad <= endRad : angleRad < endRad;
            return afterStart && beforeEnd;
        }
        else
        {
            bool afterStart = IncludeStart ? angleRad >= startRad : angleRad > startRad;
            bool beforeEnd = IncludeEnd ? angleRad <= endRad : angleRad < endRad;
            return afterStart || beforeEnd;
        }
    }

    // Проверка принадлежности другого диапазона (защита от null)
    public bool Contains(AngleRange? other)
    {
        if (other is null) return false;
        return Contains(other.Start) && Contains(other.End);
    }

    // Сравнение промежутков (безопасно для null)
    public override bool Equals(object? obj) => obj is AngleRange other &&
        Start == other.Start && End == other.End &&
        IncludeStart == other.IncludeStart && IncludeEnd == other.IncludeEnd;

    public override int GetHashCode() => HashCode.Combine(Start, End, IncludeStart, IncludeEnd);

    public static bool operator ==(AngleRange? a, AngleRange? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));
    public static bool operator !=(AngleRange? a, AngleRange? b) => !(a == b);

    // Сравнение по длине
    public static bool operator <(AngleRange a, AngleRange b) => a.Length < b.Length;
    public static bool operator >(AngleRange a, AngleRange b) => a.Length > b.Length;
    public static bool operator <=(AngleRange a, AngleRange b) => a.Length <= b.Length;
    public static bool operator >=(AngleRange a, AngleRange b) => a.Length >= b.Length;

    // Операции сложения и вычитания (объединение и разность)
    public static List<AngleRange> operator +(AngleRange a, AngleRange b) => Union(a, b);
    public static List<AngleRange> operator -(AngleRange a, AngleRange b) => Difference(a, b);

    public static List<AngleRange> Union(AngleRange a, AngleRange b)
    {
        var result = new List<AngleRange>();
        
        // Простая реализация объединения
        if (a.Overlaps(b) || a.Touches(b))
        {
            // Если промежутки пересекаются или соприкасаются, создаем один большой
            double start = Math.Min(a.Start.Radians, b.Start.Radians);
            double end = Math.Max(a.End.Radians, b.End.Radians);
            result.Add(new AngleRange(start, end));
        }
        else
        {
            // Иначе возвращаем оба промежутка
            result.Add(a);
            result.Add(b);
        }
        
        return result;
    }

    public static List<AngleRange> Difference(AngleRange a, AngleRange b)
    {
        var result = new List<AngleRange>();
        
        if (!a.Overlaps(b))
        {
            // Если не пересекаются, возвращаем первый промежуток
            result.Add(a);
        }
        else
        {
            // Упрощенная реализация разности
            if (a.Start.Radians < b.Start.Radians)
            {
                result.Add(new AngleRange(a.Start.Radians, b.Start.Radians, 
                    a.IncludeStart, !b.IncludeStart));
            }
            
            if (a.End.Radians > b.End.Radians)
            {
                result.Add(new AngleRange(b.End.Radians, a.End.Radians, 
                    !b.IncludeEnd, a.IncludeEnd));
            }
        }
        
        return result.Where(r => r.Length > 0).ToList();
    }

    private bool Overlaps(AngleRange other)
    {
        return Contains(other.Start) || Contains(other.End) || 
               other.Contains(Start) || other.Contains(End);
    }

    private bool Touches(AngleRange other)
    {
        return Math.Abs(End.Radians - other.Start.Radians) < 0.0001 ||
               Math.Abs(Start.Radians - other.End.Radians) < 0.0001;
    }

    // Строковые представления
    public override string ToString()
    {
        char startChar = IncludeStart ? '[' : '(';
        char endChar = IncludeEnd ? ']' : ')';
        return $"{startChar}{Start.ToString("rad")}, {End.ToString("rad")}{endChar}";
    }

    public string ToReprString()
    {
        return $"AngleRange(Start={Start.ToReprString()}, End={End.ToReprString()}, " +
               $"IncludeStart={IncludeStart}, IncludeEnd={IncludeEnd})";
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Демонстрация работы классов ===");
        
        // Тестирование Angle
        Console.WriteLine("\n--- Тестирование Angle ---");
        Angle angle1 = new Angle(Math.PI);
        Angle angle2 = new Angle(90, false);
        Angle angle3 = new Angle(45, false);
        
        Console.WriteLine($"angle1: {angle1} | repr: {angle1.ToReprString()}");
        Console.WriteLine($"angle2: {angle2} | repr: {angle2.ToReprString()}");
        
        // Преобразования
        Console.WriteLine($"\nПреобразования angle1:");
        Console.WriteLine($"ToFloat: {angle1.ToFloat()}");
        Console.WriteLine($"ToInt: {angle1.ToInt()}");
        Console.WriteLine($"Явное в double: {(double)angle1}");
        
        // Сравнение
        Console.WriteLine($"\nСравнение углов:");
        Console.WriteLine($"angle1 == angle2: {angle1 == angle2}");
        Console.WriteLine($"angle1 > angle2: {angle1 > angle2}");
        Console.WriteLine($"angle2 < angle3: {angle2 < angle3}");
        
        // Арифметика
        Console.WriteLine($"\nАрифметические операции:");
        Console.WriteLine($"angle2 + angle3 = {angle2 + angle3}");
        Console.WriteLine($"angle1 * 2 = {angle1 * 2}");
        Console.WriteLine($"1.5 + angle2 = {1.5 + angle2}");
        Console.WriteLine($"angle3 / 2 = {angle3 / 2}");
        
        // Тестирование AngleRange
        Console.WriteLine("\n--- Тестирование AngleRange ---");
        AngleRange range1 = new AngleRange(0, Math.PI);
        AngleRange range2 = new AngleRange(45, 135, false);
        AngleRange range3 = new AngleRange(270, 90, false);
        
        Console.WriteLine($"range1: {range1} | repr: {range1.ToReprString()}");
        Console.WriteLine($"range2: {range2}");
        Console.WriteLine($"range3: {range3}");
        Console.WriteLine($"Длина range1: {range1.Length:0.00} rad");
        
        // Принадлежность
        Console.WriteLine($"\nПроверка принадлежности:");
        Angle testAngle = new Angle(60, false);
        Console.WriteLine($"Угол 60° в range2: {range2.Contains(testAngle)}");
        Console.WriteLine($"Угол 60° в range2 (оператор in заменён на Contains): {range2.Contains(testAngle)}");
        Console.WriteLine($"range2 в range1: {range1.Contains(range2)}");
        
        // Сравнение промежутков
        Console.WriteLine($"\nСравнение промежутков:");
        Console.WriteLine($"range1 == range2: {range1 == range2}");
        Console.WriteLine($"range1 > range2: {range1 > range2}");
        
        // Операции с промежутками
        Console.WriteLine($"\nОперации с промежутками:");
        var union = AngleRange.Union(range1, range2);
        Console.WriteLine($"Объединение range1 и range2: {string.Join(" + ", union)}");
        
        var difference = AngleRange.Difference(range1, range2);
        Console.WriteLine($"Разность range1 и range2: {string.Join(" - ", difference)}");
        
        Console.WriteLine("\n=== Демонстрация завершена ===");
    }
}