using System.Text;
using System.Runtime.InteropServices;

namespace ConsolePrinterLab
{
    // simple ANSI colors
    enum Color { Black, Red, Green, Yellow, Blue, Magenta, Cyan, White, BrightWhite }

    static class Ansi
    {
        public const string Reset = "\u001b[0m";
        public static string Foreground(Color c) => c switch
        {
            Color.Black => "\u001b[30m",
            Color.Red => "\u001b[31m",
            Color.Green => "\u001b[32m",
            Color.Yellow => "\u001b[33m",
            Color.Blue => "\u001b[34m",
            Color.Magenta => "\u001b[35m",
            Color.Cyan => "\u001b[36m",
            Color.White => "\u001b[37m",
            Color.BrightWhite => "\u001b[97m",
            _ => "\u001b[37m"
        };
    }

    // Minimal font holder
    class Font
    {
        public int Height { get; }
        private readonly Dictionary<char, string[]> _glyphs;

        public Font(int height, Dictionary<char, string[]> glyphs)
        {
            Height = height;
            _glyphs = glyphs ?? new Dictionary<char, string[]>();
        }

        public string[] GetGlyph(char ch)
        {
            ch = char.ToUpperInvariant(ch);
            if (_glyphs.TryGetValue(ch, out var g)) return g;
            // fallback: blank of correct width (use width of space if present or 3)
            int w = 3;
            if (_glyphs.TryGetValue(' ', out var sp) && sp.Length == Height) w = sp[0].Length;
            var blank = new string[Height];
            for (int i = 0; i < Height; i++) blank[i] = new string(' ', w);
            return blank;
        }
    }

    static class BuiltinFonts
    {
        public static Font Font5()
        {
            var g = new Dictionary<char, string[]>();
            g['A'] = new[] { "  *  ", " * * ", "*****", "*   *", "*   *" };
            g['B'] = new[] { "**** ", "*   *", "**** ", "*   *", "**** " };
            g['C'] = new[] { " ****", "*    ", "*    ", "*    ", " ****" };
            g['D'] = new[] { "**** ", "*   *", "*   *", "*   *", "**** " };
            g['E'] = new[] { "*****", "*    ", "***  ", "*    ", "*****" };
            g['F'] = new[] { "*****", "*    ", "***  ", "*    ", "*    " };
            g['G'] = new[] { " ****", "*    ", "*  **", "*   *", " ****" };
            g['H'] = new[] { "*   *", "*   *", "*****", "*   *", "*   *" };
            g['I'] = new[] { "*****", "  *  ", "  *  ", "  *  ", "*****" };
            g['J'] = new[] { "  ***", "   * ", "   * ", "*  * ", " **  " };
            g['K'] = new[] { "*   *", "*  * ", "***  ", "*  * ", "*   *" };
            g['L'] = new[] { "*    ", "*    ", "*    ", "*    ", "*****" };
            g['M'] = new[] { "*   *", "** **", "* * *", "*   *", "*   *" };
            g['N'] = new[] { "*   *", "**  *", "* * *", "*  **", "*   *" };
            g['O'] = new[] { " *** ", "*   *", "*   *", "*   *", " *** " };
            g['P'] = new[] { "**** ", "*   *", "**** ", "*    ", "*    " };
            g['Q'] = new[] { " *** ", "*   *", "*   *", "*  **", " ****" };
            g['R'] = new[] { "**** ", "*   *", "**** ", "*  * ", "*   *" };
            g['S'] = new[] { " ****", "*    ", " *** ", "    *", "**** " };
            g['T'] = new[] { "*****", "  *  ", "  *  ", "  *  ", "  *  " };
            g['U'] = new[] { "*   *", "*   *", "*   *", "*   *", " *** " };
            g['V'] = new[] { "*   *", "*   *", "*   *", " * * ", "  *  " };
            g['W'] = new[] { "*   *", "*   *", "* * *", "** **", "*   *" };
            g['X'] = new[] { "*   *", " * * ", "  *  ", " * * ", "*   *" };
            g['Y'] = new[] { "*   *", " * * ", "  *  ", "  *  ", "  *  " };
            g['Z'] = new[] { "*****", "   * ", "  *  ", " *   ", "*****" };
            g['0'] = new[] { " *** ", "*  **", "* * *", "**  *", " *** " };
            g['1'] = new[] { "  *  ", " **  ", "  *  ", "  *  ", " *** " };
            g['2'] = new[] { " *** ", "*   *", "  ** ", " *   ", "*****" };
            g['3'] = new[] { " *** ", "    *", "  ** ", "    *", " *** " };
            g['4'] = new[] { "*   *", "*   *", "*****", "    *", "    *" };
            g['5'] = new[] { "*****", "*    ", "**** ", "    *", "**** " };
            g['6'] = new[] { " ****", "*    ", "**** ", "*   *", " *** " };
            g['7'] = new[] { "*****", "   * ", "  *  ", " *   ", "*    " };
            g['8'] = new[] { " *** ", "*   *", " *** ", "*   *", " *** " };
            g['9'] = new[] { " *** ", "*   *", " ****", "    *", " *** " };
            g[' '] = new[] { "     ", "     ", "     ", "     ", "     " };
            return new Font(5, g);
        }

        public static Font Font7()
        {
            var g = new Dictionary<char, string[]>();
            g['A'] = new[] { "   *   ", "  * *  ", " *   * ", " *   * ", "*******", "*     *", "*     *" };
            g['B'] = new[] { "****** ", "*     *", "*     *", "****** ", "*     *", "*     *", "****** " };
            g['C'] = new[] { "  *****", " *     ", "*      ", "*      ", "*      ", " *     ", "  *****" }.Select(s => s.Length>7 ? s.Substring(0,7) : s.PadLeft((7+s.Length)/2).PadRight(7)).ToArray();
            g['D'] = new[] { "****** ", "*     *", "*     *", "*     *", "*     *", "*     *", "****** " };
            g['E'] = new[] { "*******", "*      ", "*      ", "*****  ", "*      ", "*      ", "*******" };
            g['F'] = new[] { "*******", "*      ", "*      ", "*****  ", "*      ", "*      ", "*      " };
            g['G'] = new[] { "  *****", " *     ", "*      ", "*  ****", "*     *", " *    *", "  **** " }.Select(s => s.Length>7 ? s.Substring(0,7) : s.PadLeft((7+s.Length)/2).PadRight(7)).ToArray();
            g['H'] = new[] { "*     *", "*     *", "*     *", "*******", "*     *", "*     *", "*     *" };
            g['I'] = new[] { "*******", "   *   ", "   *   ", "   *   ", "   *   ", "   *   ", "*******" };
            g['J'] = new[] { "  *****", "     * ", "     * ", "     * ", "*    * ", " *   * ", "  ***  " }.Select(s => s.Length>7 ? s.Substring(0,7) : s.PadLeft((7+s.Length)/2).PadRight(7)).ToArray();
            g['K'] = new[] { "*    * ", "*   *  ", "*  *   ", "***    ", "*  *   ", "*   *  ", "*    * " };
            g['L'] = new[] { "*      ", "*      ", "*      ", "*      ", "*      ", "*      ", "*******" };
            g['M'] = new[] { "*     *", "**   **", "* * * *", "*  *  *", "*     *", "*     *", "*     *" };
            g['N'] = new[] { "*     *", "**    *", "* *   *", "*  *  *", "*   * *", "*    **", "*     *" };
            g['O'] = new[] { "  ***  ", " *   * ", "*     *", "*     *", "*     *", " *   * ", "  ***  " };
            g['P'] = new[] { "****** ", "*     *", "*     *", "****** ", "*      ", "*      ", "*      " };
            g['Q'] = new[] { "  ***  ", " *   * ", "*     *", "*     *", "*   * *", " *   * ", "  **** " };
            g['R'] = new[] { "****** ", "*     *", "*     *", "****** ", "*   *  ", "*    * ", "*     *" };
            g['S'] = new[] { " ***** ", "*      ", "*      ", " ***** ", "      *", "      *", " ***** " };
            g['T'] = new[] { "*******", "   *   ", "   *   ", "   *   ", "   *   ", "   *   ", "   *   " };
            g['U'] = new[] { "*     *", "*     *", "*     *", "*     *", "*     *", "*     *", " ***** " };
            g['V'] = new[] { "*     *", "*     *", "*     *", " *   * ", "  * *  ", "   *   ", "   *   " };
            g['W'] = new[] { "*     *", "*     *", "*     *", "*  *  *", "* * * *", "**   **", "*     *" };
            g['X'] = new[] { "*     *", " *   * ", "  * *  ", "   *   ", "  * *  ", " *   * ", "*     *" };
            g['Y'] = new[] { "*     *", " *   * ", "  * *  ", "   *   ", "   *   ", "   *   ", "   *   " };
            g['Z'] = new[] { "*******", "     * ", "    *  ", "   *   ", "  *    ", " *     ", "*******" };
            // digits (basic)
            g['0'] = new[] { "  ***  ", " *   * ", "*  ** *", "* *  * ", "*  ** *", " *   * ", "  ***  " };
            g['1'] = new[] { "   *   ", "  **   ", " * *   ", "   *   ", "   *   ", "   *   ", " ***** " };
            g['2'] = new[] { "  ***  ", " *   * ", "*     *", "    ** ", "  **   ", " *     ", "*******" };
            g['3'] = new[] { "  ***  ", " *   * ", "     * ", "   **  ", "     * ", " *   * ", "  ***  " };
            g['4'] = new[] { "    *  ", "   **  ", "  * *  ", " *  *  ", "*******", "    *  ", "    *  " };
            g['5'] = new[] { "*******", "*      ", "****** ", "      *", "      *", "*     *", " ***** " };
            g['6'] = new[] { "  **** ", " *     ", "*      ", "****** ", "*     *", "*     *", " ***** " };
            g['7'] = new[] { "*******", "     * ", "    *  ", "   *   ", "  *    ", " *     ", "*      " };
            g['8'] = new[] { " ***** ", "*     *", "*     *", " ***** ", "*     *", "*     *", " ***** " };
            g['9'] = new[] { " ***** ", "*     *", "*     *", " ******", "      *", "     * ", " ****  " };
            g[' '] = new[] { "       ", "       ", "       ", "       ", "       ", "       ", "       " };
            return new Font(7, g);
        }
    }

    static class ConsoleHelper
    {
        // Try enable VT on Windows (best-effort)
        public static void EnableVirtualTerminal()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
                const int STD_OUTPUT_HANDLE = -11;
                const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(handle, out uint mode))
                {
                    SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
                }
            }
            catch { }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    }

    // Simple printer: static and instance usage, safe cursor clamped to window
    class Printer : IDisposable
    {
        private readonly Color _color;
        private readonly (int x, int y) _pos;
        private readonly char _symbol;
        private readonly Font _font;
        private readonly int _savedLeft;
        private readonly int _savedTop;
        private bool _disposed;
        private static readonly object _lock = new object();

        public Printer(Color color, (int x, int y) position, char symbol, Font font)
        {
            _color = color; _pos = position; _symbol = symbol; _font = font;
            try { _savedLeft = Console.CursorLeft; _savedTop = Console.CursorTop; } catch { _savedLeft = 0; _savedTop = 0; }
            ConsoleHelper.EnableVirtualTerminal();
        }

        // static method
        public static void Print(string text, Color color, (int x, int y) position, char symbol, Font font)
        {
            ConsoleHelper.EnableVirtualTerminal();
            var code = Ansi.Foreground(color);
            int x0 = Math.Max(0, position.x);
            int y0 = Math.Max(0, position.y);

            lock (_lock)
            {
                int winW = Math.Max(1, Console.WindowWidth);
                int winH = Math.Max(1, Console.WindowHeight);

                for (int row = 0; row < font.Height; row++)
                {
                    int y = y0 + row;
                    if (y < 0 || y >= winH) continue;

                    var sb = new StringBuilder();
                    foreach (var ch in text)
                    {
                        var glyph = font.GetGlyph(ch);
                        var line = glyph[row];
                        foreach (var p in line) sb.Append(p == '*' ? symbol : ' ');
                        sb.Append(' ');
                        if (sb.Length >= winW) break;
                    }

                    var content = sb.ToString();
                    if (string.IsNullOrEmpty(content)) continue;

                    int safeX = Math.Max(0, Math.Min(winW - 1, x0));
                    int maxLen = Math.Max(0, winW - safeX);

                    // ВАЖНО: обрезаем видимый контент до maxLen ПЕРЕД добавлением ANSI-кодов
                    if (content.Length > maxLen) content = content.Substring(0, maxLen);

                    var colored = code + content + Ansi.Reset;

                    try { Console.SetCursorPosition(safeX, y); Console.Write(colored); }
                    catch { try { Console.Write(colored); } catch { } }
                }
            }
        }

        public void Print(string text) => Print(text, _color, _pos, _symbol, _font);

        public void Dispose()
        {
            if (_disposed) return;
            lock (_lock)
            {
                try
                {
                    int winW = Math.Max(1, Console.WindowWidth);
                    int winH = Math.Max(1, Console.WindowHeight);
                    int left = Math.Max(0, Math.Min(winW - 1, _savedLeft));
                    int top = Math.Max(0, Math.Min(winH - 1, _savedTop));
                    Console.SetCursorPosition(left, top);
                    Console.Write(Ansi.Reset);
                }
                catch { }
                _disposed = true;
            }
        }
    }

    class Program
    {
        static void Main()
        {
            // Simple demo: clear and draw two banners (font5 and font7)
            ConsoleHelper.EnableVirtualTerminal();
            try { Console.Clear(); } catch { }

            var f5 = BuiltinFonts.Font5();
            var f7 = BuiltinFonts.Font7();

            // static usage
            Printer.Print("Hello", Color.Magenta, (2, 1), '#', f7);
            Printer.Print("World", Color.Blue, (2, 8), '@', f7);
            try { Console.SetCursorPosition(0, Math.Min(Console.WindowHeight - 1, 25)); } catch { }
            Console.WriteLine("\nDone - press any key.");
            Console.ReadKey(true);
        }
    }
}