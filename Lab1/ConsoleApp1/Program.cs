using System;
using System.Collections.Generic;
using System.Linq;

public class Angle
{
    private double radians; // raw stored radians (may be outside [0,2π))
    private const double TwoPi = 2 * Math.PI;
    private const double Eps = 1e-9;

    public Angle(double value = 0, bool isRadians = true, bool normalize = true)
    {
        if (isRadians)
            radians = normalize ? Normalize(value) : value;
        else
            radians = normalize ? Normalize(DegreesToRadians(value)) : DegreesToRadians(value);
    }

    // Raw radians (как сохранено)
    public double RawRadians => radians;

    // Нормализованное значение в [0, 2π)
    public double NormalizedRadians => Normalize(radians);

    public double Radians
    {
        get => radians;
        set => radians = value;
    }

    public double Degrees
    {
        get => RadiansToDegrees(radians);
        set => radians = DegreesToRadians(value);
    }

    // Нормализация вспомогательная
    private double Normalize(double rad)
    {
        double result = rad % TwoPi;
        if (result < 0)
            result += TwoPi;
        return result;
    }

    private double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
    private double RadiansToDegrees(double rad) => rad * 180.0 / Math.PI;

    public float ToFloat() => (float)radians;
    public int ToInt() => (int)Math.Round(radians);

    // ToString показывает фактическое значение.
    public override string ToString() => $"{Degrees:0.0}°";

    public string ToString(string format) => format.ToLower() switch
    {
        "rad" or "radians" => $"{radians:0.00} rad",
        "deg" or "degrees" => $"{Degrees:0.0}°",
        _ => ToString()
    };

    public string ToReprString() =>
        $"Angle(raw_radians={radians:0.0000}, normalized={NormalizedRadians:0.0000}, degrees={Degrees:0.00}°)";

    // Equals сравнивает по нормализованному значению (углы эквивалентны по модулю 2π)
    public override bool Equals(object? obj) =>
        obj is Angle other && Math.Abs(NormalizedRadians - other.NormalizedRadians) < Eps;

    public override int GetHashCode() => NormalizedRadians.GetHashCode();

    public static bool operator ==(Angle? a, Angle? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));
    public static bool operator !=(Angle? a, Angle? b) => !(a == b);
    public static bool operator <(Angle a, Angle b) => a.NormalizedRadians < b.NormalizedRadians;
    public static bool operator >(Angle a, Angle b) => a.NormalizedRadians > b.NormalizedRadians;
    public static bool operator <=(Angle a, Angle b) => a.NormalizedRadians <= b.NormalizedRadians;
    public static bool operator >=(Angle a, Angle b) => a.NormalizedRadians >= b.NormalizedRadians;

    // Арифметика — сохраняем "сырые" суммы/произведения
    public static Angle operator +(Angle a, Angle b) => new Angle(a.radians + b.radians, true, false);
    public static Angle operator +(Angle a, double b) => new Angle(a.radians + b, true, false);
    public static Angle operator +(Angle a, int b) => new Angle(a.radians + b, true, false);
    public static Angle operator +(double a, Angle b) => new Angle(a + b.radians, true, false);
    public static Angle operator +(int a, Angle b) => new Angle(a + b.radians, true, false);

    public static Angle operator -(Angle a, Angle b) => new Angle(a.radians - b.radians, true, false);
    public static Angle operator -(Angle a, double b) => new Angle(a.radians - b, true, false);
    public static Angle operator -(Angle a, int b) => new Angle(a.radians - b, true, false);

    public static Angle operator *(Angle a, double num) => new Angle(a.radians * num, true, false);
    public static Angle operator *(Angle a, int num) => new Angle(a.radians * num, true, false);
    public static Angle operator *(double num, Angle a) => new Angle(a.radians * num, true, false);
    public static Angle operator *(int num, Angle a) => new Angle(a.radians * num, true, false);

    public static Angle operator /(Angle a, double num) => new Angle(a.radians / num, true, false);
    public static Angle operator /(Angle a, int num) => new Angle(a.radians / num, true, false);

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

    private const double TwoPi = 2 * Math.PI;
    private const double Eps = 1e-9;

    public AngleRange(Angle start, Angle end, bool includeStart = true, bool includeEnd = true)
    {
        Start = start;
        End = end;
        IncludeStart = includeStart;
        IncludeEnd = includeEnd;
    }

    public AngleRange(double start, double end, bool isRadians = true,
                     bool includeStart = true, bool includeEnd = true)
        : this(new Angle(start, isRadians, false), new Angle(end, isRadians, false), includeStart, includeEnd)
    {
    }

    // Длина промежутка вдоль положительного направления от Start до End.
    public double GetLength()
    {
        double delta = End.RawRadians - Start.RawRadians;
        // Подтягиваем вверх, чтобы delta >= 0, но не уменьшаем (если End выше Start — оставляем)
        while (delta < 0) delta += TwoPi;
        return delta;
    }

    // Синоним‑свойство для удобства (используется в форматировании/фильтрации)
    public double Length => GetLength();

    // Проверка принадлежности одного угла к промежутку (учитываем \pm 2π сдвиги).
    public bool Contains(Angle angle)
    {
        double s = Start.RawRadians;
        double e = s + GetLength(); // эффективный правый конец в сырой шкале (>= s)
        double x = angle.RawRadians;

        // ищем целое m такое, что x + m*2π ∈ [s, e]
        double mMin = Math.Ceiling((s - x - Eps) / TwoPi);
        double mMax = Math.Floor((e - x + Eps) / TwoPi);
        if (mMin > mMax) return false;

        // возьмём m = mMin как кандидат
        double sx = x + mMin * TwoPi;
        if (Math.Abs(sx - s) < Eps) return IncludeStart;           // попадает ровно в левую границу
        if (Math.Abs(sx - e) < Eps) return IncludeEnd;             // ровно в правую границу
        if (sx > s + Eps && sx < e - Eps) return true;             // строго внутри
        // если подходят другие m (в диапазоне), тогда обязательно внутри
        if (mMax > mMin) return true;
        return false;
    }

    // Проверка принадлежности другого диапазона (ищем сдвиг m*2π, при котором другой полностью внутри)
    public bool Contains(AngleRange? other)
    {
        if (other is null) return false;

        double s = this.Start.RawRadians;
        double e = s + this.GetLength();
        double os = other.Start.RawRadians;
        double oe = os + other.GetLength();

        // нужно найти m такое, что s <= os + m*2π  и  oe + m*2π <= e
        double mMin = Math.Ceiling((s - os - Eps) / TwoPi);
        double mMax = Math.Floor((e - oe + Eps) / TwoPi);
        if (mMin > mMax) return false;

        // проверим граничные включения для выбранного m (берём m = mMin)
        double shiftedStart = os + mMin * TwoPi;
        double shiftedEnd = oe + mMin * TwoPi;

        // если левый совпадает с s — проверяем IncludeStart обеих
        if (Math.Abs(shiftedStart - s) < Eps && !(this.IncludeStart && other.IncludeStart)) return false;
        // если правый совпадает с e — проверяем IncludeEnd обеих
        if (Math.Abs(shiftedEnd - e) < Eps && !(this.IncludeEnd && other.IncludeEnd)) return false;

        // если оба границы терпимы — содержится
        if (shiftedStart > s + Eps && shiftedEnd < e - Eps) return true;
        // если равен по длине полностью — проверяем включения обоих краёв
        if (Math.Abs(shiftedStart - s) < Eps && Math.Abs(shiftedEnd - e) < Eps)
            return this.IncludeStart == other.IncludeStart && this.IncludeEnd == other.IncludeEnd;

        // остальные случаи: если попали сюда, то mMin<=mMax и нет явных нарушений — считаем, что содержится
        return true;
    }

    private bool Touches(AngleRange other)
    {
        // Проверяем, совпадают ли границы с учётом сдвигов на кратные 2π
        bool EqMod(double a, double b)
        {
            double k = Math.Round((a - b) / TwoPi);
            return Math.Abs(a - (b + k * TwoPi)) < Eps;
        }

        if (EqMod(this.End.RawRadians, other.Start.RawRadians)) return true;
        if (EqMod(this.Start.RawRadians, other.End.RawRadians)) return true;
        return false;
    }

    // Чёткие строковые представления с явными числовыми значениями (для удобной проверки)
    public override string ToString()
    {
        char startChar = IncludeStart ? '[' : '(';
        char endChar = IncludeEnd ? ']' : ')';
        // Показываем сырые радианы и градусы — удобно для тестов
        return $"{startChar}{Start.ToString("rad")} ({Start.ToString("deg")}), {End.ToString("rad")} ({End.ToString("deg")}){endChar}";
    }

    public string ToReprString()
    {
        return $"AngleRange(Start={Start.ToReprString()}, End={End.ToReprString()}, " +
               $"IncludeStart={IncludeStart}, IncludeEnd={IncludeEnd}, Length={GetLength():0.0000})";
    }

    // Возвращает объединение двух диапазонов как список: один элемент при перекрытии/касании, иначе два (отсортированных).
    public static List<AngleRange> Union(AngleRange a, AngleRange b)
    {
        if (a is null || b is null) return new List<AngleRange>();

        // Если один содержит другой — возвращаем контейнер
        if (a.Contains(b)) return new List<AngleRange> { a };
        if (b.Contains(a)) return new List<AngleRange> { b };

        double s1 = a.Start.RawRadians;
        double e1 = s1 + a.GetLength();
        double s2 = b.Start.RawRadians;
        double e2 = s2 + b.GetLength();

        // Попытаемся сдвинуть второй диапазон на ближайший кратный 2π, чтобы проверить перекрытие/касание.
        int mCenter = (int)Math.Round((s1 - s2) / TwoPi);

        for (int dm = -1; dm <= 1; dm++)
        {
            int m = mCenter + dm;
            double s2m = s2 + m * TwoPi;
            double e2m = e2 + m * TwoPi;

            // Проверяем, не являются ли интервалы раздельными (с зазором).
            if (!(e1 < s2m - Eps || e2m < s1 - Eps))
            {
                double ns = Math.Min(s1, s2m);
                double ne = Math.Max(e1, e2m);

                bool nIncludeStart;
                if (Math.Abs(ns - s1) < Eps && Math.Abs(ns - s2m) < Eps)
                    nIncludeStart = a.IncludeStart || b.IncludeStart;
                else if (Math.Abs(ns - s1) < Eps)
                    nIncludeStart = a.IncludeStart;
                else
                    nIncludeStart = b.IncludeStart;

                bool nIncludeEnd;
                if (Math.Abs(ne - e1) < Eps && Math.Abs(ne - e2m) < Eps)
                    nIncludeEnd = a.IncludeEnd || b.IncludeEnd;
                else if (Math.Abs(ne - e1) < Eps)
                    nIncludeEnd = a.IncludeEnd;
                else
                    nIncludeEnd = b.IncludeEnd;

                var merged = new AngleRange(new Angle(ns, true, false), new Angle(ne, true, false), nIncludeStart, nIncludeEnd);
                return new List<AngleRange> { merged };
            }
        }

        // Раздельные интервалы: возвращаем оба в порядке по эффективному положению (связываем b к ближайшему mCenter для сравнения порядка).
        double s2mFinal = s2 + mCenter * TwoPi;

        if (s1 <= s2mFinal)
            return new List<AngleRange> { a, b };
        else
            return new List<AngleRange> { b, a };
    }
}

class Program
{
    static void Main()
    {
        // Короткий, читабельный вывод: только то, что нужно.
        // Помощники для компактного отображения
        string Deg(Angle a) => $"{a.Degrees:0.##}°";
        string Rad(Angle a) => $"{a.RawRadians:0.###} rad";
        string AngleShort(Angle a, bool showDegreesOnly = false) => showDegreesOnly ? Deg(a) : $"{Deg(a)} / {Rad(a)}";
        string RangeShort(AngleRange r, bool showDegreesOnly = false)
            => showDegreesOnly
               ? $"{(r.IncludeStart ? "[" : "(")}{Deg(r.Start)} .. {Deg(r.End)}{(r.IncludeEnd ? "]" : ")")}"
               : $"{(r.IncludeStart ? "[" : "(")}{Rad(r.Start)} .. {Rad(r.End)}{(r.IncludeEnd ? "]" : ")")}  ({r.Length:0.###} rad)";

        Console.WriteLine("=== КРАТКАЯ ДЕМО-ВЫВОД ===\n");

        // Создаём объекты (как раньше)
        Angle angle1 = new Angle(Math.PI);          // radians
        Angle angle2 = new Angle(90, false);        // degrees
        Angle angle3 = new Angle(45, false);        // degrees

        AngleRange range1 = new AngleRange(0, Math.PI);          // numeric radians (raw)
        AngleRange range2 = new AngleRange(45, 135, false);      // degrees input -> show degrees
        AngleRange range3 = new AngleRange(270, 90, false);      // wrap, degrees input

        Console.WriteLine("-- Углы --");
        Console.WriteLine($"angle1: {AngleShort(angle1)}");
        Console.WriteLine($"angle2: {AngleShort(angle2, showDegreesOnly: true)}"); // был задан в градусах
        Console.WriteLine($"angle3: {AngleShort(angle3, showDegreesOnly: true)}");
        Console.WriteLine();

        Console.WriteLine("-- Сравнения (показываем значения) --");
        Console.WriteLine($"{AngleShort(angle1)} == {AngleShort(angle2, true)} -> {angle1 == angle2}");
        Console.WriteLine($"{AngleShort(angle2, true)} <  {AngleShort(angle3, true)} -> {angle2 < angle3}");
        Console.WriteLine();

        Console.WriteLine("-- Промежутки (компактно) --");
        Console.WriteLine($"range1: {RangeShort(range1)}");
        Console.WriteLine($"range2: {RangeShort(range2, showDegreesOnly: true)}");
        Console.WriteLine($"range3: {RangeShort(range3, showDegreesOnly: true)}");
        Console.WriteLine();

        Console.WriteLine("-- Принадлежность (компактно) --");
        Angle testAngle = new Angle(60, false); // 60°
        Console.WriteLine($"{AngleShort(testAngle, true)} ∈ range2? -> {range2.Contains(testAngle)}");
        Console.WriteLine($"{RangeShort(range2, true)} ⊂ {RangeShort(range1)}? -> {range1.Contains(range2)}");
        Console.WriteLine();

        Console.WriteLine("-- Пример вложенности (показываем СЫРЫЕ значения для наглядности) --");
        AngleRange big = new AngleRange(Math.PI / 2.0, 6 * Math.PI); // [π/2 ; 6π] 
        AngleRange small = new AngleRange(Math.PI / 3.0, 3 * Math.PI, true, false, false); // (π/3 ; 3π)
        // Показываем raw радианы + удобные градусы где уместно
        Console.WriteLine($"big:   {RangeShort(big)}");
        Console.WriteLine($"small: {RangeShort(small)}");
        Console.WriteLine($"small ⊂ big? -> {big.Contains(small)}");
        Console.WriteLine();

        Console.WriteLine("-- Объединение / Разность (компактно) --");
        var u = AngleRange.Union(range1, range2);
        Console.WriteLine($"Объединение {RangeShort(range1, showDegreesOnly: true)} и {RangeShort(range2, showDegreesOnly: true)}:");
                if (u.Count == 0) Console.WriteLine("  (пусто)");
                else foreach (var item in u) Console.WriteLine($"  - {RangeShort(item, showDegreesOnly: true)}");
        
            }
        }