using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tic;
using NFun.Tokenization;

namespace NFun.ConsoleApp;

class Program {
    // ── Session context (#10) ──────────────────────────────────────────────────
    static readonly List<string> _sessionDefinitions = new();

    // ── History ────────────────────────────────────────────────────────────────
    static readonly List<string[]> _history = new();
    static int _historyIndex = -1;
    static readonly string _historyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfun_history");

    // ── Editor state ───────────────────────────────────────────────────────────
    static int _startTop;
    static int _prevLineCount;
    const int PromptLen = 2;
    const int TabSize = 4;
    const int MaxHistoryEntries = 500;
    const int MaxUndoSteps = 100;

    // ── Undo/Redo (#1) ────────────────────────────────────────────────────────
    struct EditorSnapshot {
        public string[] Lines;
        public int Row, Col;
    }

    static readonly List<EditorSnapshot> _undoStack = new();
    static readonly List<EditorSnapshot> _redoStack = new();

    static void PushUndo(List<string> lines, int row, int col) {
        _undoStack.Add(new EditorSnapshot { Lines = lines.ToArray(), Row = row, Col = col });
        if (_undoStack.Count > MaxUndoSteps)
            _undoStack.RemoveAt(0);
        _redoStack.Clear();
    }

    static bool TryUndo(List<string> lines, ref int row, ref int col) {
        if (_undoStack.Count == 0) return false;
        _redoStack.Add(new EditorSnapshot { Lines = lines.ToArray(), Row = row, Col = col });
        var snap = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        lines.Clear();
        lines.AddRange(snap.Lines);
        row = snap.Row;
        col = snap.Col;
        return true;
    }

    static bool TryRedo(List<string> lines, ref int row, ref int col) {
        if (_redoStack.Count == 0) return false;
        _undoStack.Add(new EditorSnapshot { Lines = lines.ToArray(), Row = row, Col = col });
        var snap = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        lines.Clear();
        lines.AddRange(snap.Lines);
        row = snap.Row;
        col = snap.Col;
        return true;
    }

    // ── Main ───────────────────────────────────────────────────────────────────
    static int Main(string[] args) {
        var argList = args.ToList();
        if (argList.Remove("-t") || argList.Remove("--trace"))
            TraceLog.IsEnabled = true;

        if (argList.Count >= 2 && argList[0] is "-e" or "--eval")
        {
            var expression = string.Join(" ", argList.Skip(1));
            return ExecuteNonInteractive(expression);
        }

        if (argList.Count >= 2 && argList[0] is "-s" or "--script")
        {
            var script = File.ReadAllText(argList[1]);
            return ExecuteNonInteractive(script);
        }

        if (argList.Count == 1 && argList[0] is "-h" or "--help")
        {
            Console.WriteLine("Usage: nfun [options]");
            Console.WriteLine("  (no args)          Interactive REPL");
            Console.WriteLine("  -e, --eval <expr>  Evaluate expression and print results");
            Console.WriteLine("  -s, --script <file> Run script from file");
            Console.WriteLine("  -t, --trace        Show TIC solver trace");
            Console.WriteLine("  -h, --help         Show this help");
            return 0;
        }

        LoadHistory();
        PrintWelcome();
        while (true)
        {
            var expr = ReadExpression();
            if (expr == null)
                break;
            if (expr.Length == 0)
                continue;
            if (TryHandleCommand(expr))
                continue;
            Execute(expr);
        }
        SaveHistory();
        return 0;
    }

    static void PrintWelcome() {
        Write("NFun", ConsoleColor.Cyan);
        Console.Write(" Playground  ");
        WriteDim("Alt+Enter = run, /help for commands");
        Console.WriteLine();
    }

    // ── Persistent history (#5) ────────────────────────────────────────────────
    static void LoadHistory() {
        try {
            if (!File.Exists(_historyPath)) return;
            var json = File.ReadAllText(_historyPath);
            // Simple format: entries separated by \0, lines within entry separated by \n
            foreach (var entry in json.Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                var lines = entry.Split('\n');
                if (lines.Length > 0 && lines.Any(l => l.Trim().Length > 0))
                    _history.Add(lines);
            }
        }
        catch { /* ignore corrupt history */ }
    }

    static void SaveHistory() {
        try {
            var entries = _history.TakeLast(MaxHistoryEntries);
            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                sb.Append(string.Join("\n", entry));
                sb.Append('\0');
            }
            File.WriteAllText(_historyPath, sb.ToString());
        }
        catch { /* ignore write errors */ }
    }

    // ── Multi-line editor ──────────────────────────────────────────────────────
    static string ReadExpression() {
        var lines = new List<string> { "" };
        int row = 0, col = 0;
        _historyIndex = _history.Count;
        string[] savedInput = null;
        _startTop = Console.CursorTop;
        _prevLineCount = 0;
        _undoStack.Clear();
        _redoStack.Clear();

        Redraw(lines, row, col, forceAll: true);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            var ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
            var alt = key.Modifiers.HasFlag(ConsoleModifiers.Alt);

            // ── Special combos (before switch) ─────────────────────────
            // Alt+Enter = execute
            if (key.Key == ConsoleKey.Enter && alt)
            {
                Console.CursorVisible = true;
                Console.SetCursorPosition(0, _startTop + lines.Count);
                Console.WriteLine();
                return FinishInput(lines);
            }
            // Ctrl+C = cancel
            if (key.Key == ConsoleKey.C && ctrl)
            {
                Console.CursorVisible = true;
                Console.SetCursorPosition(0, _startTop + lines.Count);
                Console.WriteLine();
                return "";
            }
            // Ctrl+D on empty = exit
            if (key.Key == ConsoleKey.D && ctrl)
            {
                if (lines.Count == 1 && lines[0].Length == 0)
                {
                    Console.CursorVisible = true;
                    Console.SetCursorPosition(0, _startTop + lines.Count);
                    Console.WriteLine();
                    return null;
                }
                continue;
            }
            // Ctrl+Z = undo (#1)
            if (key.Key == ConsoleKey.Z && ctrl)
            {
                TryUndo(lines, ref row, ref col);
                Redraw(lines, row, col, forceAll: true);
                continue;
            }
            // Ctrl+Y = redo (#1)
            if (key.Key == ConsoleKey.Y && ctrl)
            {
                TryRedo(lines, ref row, ref col);
                Redraw(lines, row, col, forceAll: true);
                continue;
            }
            // Ctrl+L = clear screen (#3)
            if (key.Key == ConsoleKey.L && ctrl)
            {
                Console.Clear();
                _startTop = 0;
                Redraw(lines, row, col, forceAll: true);
                continue;
            }
            // Ctrl+K = kill to end of line (#9)
            if (key.Key == ConsoleKey.K && ctrl)
            {
                if (col < lines[row].Length)
                {
                    PushUndo(lines, row, col);
                    lines[row] = lines[row][..col];
                }
                else if (row < lines.Count - 1)
                {
                    PushUndo(lines, row, col);
                    lines[row] += lines[row + 1];
                    lines.RemoveAt(row + 1);
                }
                Redraw(lines, row, col, forceAll: true);
                continue;
            }
            // Ctrl+A = select all → clear (#2)
            if (key.Key == ConsoleKey.A && ctrl)
            {
                PushUndo(lines, row, col);
                lines.Clear();
                lines.Add("");
                row = 0;
                col = 0;
                Redraw(lines, row, col, forceAll: true);
                continue;
            }

            bool lineCountChanged = false;
            int oldRow = row;
            bool textChanged = false;

            // Snapshot for undo BEFORE any modification
            var undoSnap = new EditorSnapshot { Lines = lines.ToArray(), Row = row, Col = col };

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                {
                    var before = lines[row][..col];
                    var after = lines[row][col..];
                    lines[row] = before;
                    // Auto-indent (#7): carry indent from current line + extra for open brackets
                    var indent = GetIndent(before);
                    if (before.TrimEnd().EndsWith("[") || before.TrimEnd().EndsWith("{")
                        || before.TrimEnd().EndsWith("("))
                        indent += new string(' ', TabSize);
                    lines.Insert(row + 1, indent + after);
                    row++;
                    col = indent.Length;
                    lineCountChanged = true;
                    break;
                }

                case ConsoleKey.Backspace:
                    if (alt) // Alt+Backspace = delete word backward (#4)
                    {
                        if (col > 0)
                        {
                            textChanged = true;
                            var newCol = FindWordBoundaryLeft(lines[row], col);
                            lines[row] = lines[row].Remove(newCol, col - newCol);
                            col = newCol;
                        }
                    }
                    else if (col > 0)
                    {
                        textChanged = true;
                        lines[row] = lines[row].Remove(col - 1, 1);
                        col--;
                    }
                    else if (row > 0)
                    {
                        textChanged = true;
                        col = lines[row - 1].Length;
                        lines[row - 1] += lines[row];
                        lines.RemoveAt(row);
                        row--;
                        lineCountChanged = true;
                    }
                    break;

                case ConsoleKey.Delete:
                    if (col < lines[row].Length)
                    {
                        textChanged = true;
                        lines[row] = lines[row].Remove(col, 1);
                    }
                    else if (row < lines.Count - 1)
                    {
                        textChanged = true;
                        lines[row] += lines[row + 1];
                        lines.RemoveAt(row + 1);
                        lineCountChanged = true;
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (alt)
                        col = FindWordBoundaryLeft(lines[row], col);
                    else if (col > 0) col--;
                    else if (row > 0) { row--; col = lines[row].Length; }
                    break;

                case ConsoleKey.RightArrow:
                    if (alt)
                        col = FindWordBoundaryRight(lines[row], col);
                    else if (col < lines[row].Length) col++;
                    else if (row < lines.Count - 1) { row++; col = 0; }
                    break;

                case ConsoleKey.UpArrow:
                    if (row > 0)
                    {
                        row--;
                        col = Math.Min(col, lines[row].Length);
                    }
                    else if (_history.Count > 0 && _historyIndex > 0)
                    {
                        if (_historyIndex == _history.Count)
                            savedInput = lines.ToArray();
                        _historyIndex--;
                        LoadFromHistory(lines, _history[_historyIndex], out row, out col);
                        lineCountChanged = true;
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (row < lines.Count - 1)
                    {
                        row++;
                        col = Math.Min(col, lines[row].Length);
                    }
                    else if (_historyIndex < _history.Count)
                    {
                        _historyIndex++;
                        var src = _historyIndex < _history.Count
                            ? _history[_historyIndex]
                            : savedInput ?? new[] { "" };
                        LoadFromHistory(lines, src, out row, out col);
                        lineCountChanged = true;
                    }
                    break;

                case ConsoleKey.Home:
                    col = 0;
                    break;

                case ConsoleKey.End:
                    col = lines[row].Length;
                    break;

                case ConsoleKey.Tab:
                {
                    textChanged = true;
                    var spaces = new string(' ', TabSize - (col % TabSize));
                    lines[row] = lines[row].Insert(col, spaces);
                    col += spaces.Length;
                    break;
                }

                case ConsoleKey.Escape:
                    PushUndo(lines, row, col);
                    lines.Clear();
                    lines.Add("");
                    row = 0;
                    col = 0;
                    lineCountChanged = true;
                    break;

                default:
                    // Don't insert chars when Alt is held (macOS sends special chars)
                    if (key.KeyChar >= ' ' && !alt)
                    {
                        textChanged = true;
                        lines[row] = lines[row].Insert(col, key.KeyChar.ToString());
                        col++;
                    }
                    break;
            }

            if (textChanged)
            {
                _undoStack.Add(undoSnap);
                if (_undoStack.Count > MaxUndoSteps)
                    _undoStack.RemoveAt(0);
                _redoStack.Clear();
            }

            if (col > lines[row].Length) col = lines[row].Length;

            if (lineCountChanged || row != oldRow)
                Redraw(lines, row, col, forceAll: true, textChanged: textChanged);
            else
                Redraw(lines, row, col, forceAll: false, textChanged: textChanged);
        }
    }

    static string GetIndent(string line) {
        int i = 0;
        while (i < line.Length && line[i] == ' ') i++;
        return line[..i];
    }

    static string FinishInput(List<string> lines) {
        while (lines.Count > 1 && lines[^1].Trim().Length == 0)
            lines.RemoveAt(lines.Count - 1);
        while (lines.Count > 1 && lines[0].Trim().Length == 0)
            lines.RemoveAt(0);

        var result = string.Join("\n", lines);

        if (result.Trim().Length > 0)
        {
            _history.Add(lines.ToArray());
            SaveHistory();
        }

        return result;
    }

    // ── Rendering ──────────────────────────────────────────────────────────────
    static ConsoleColor[] _cachedColorMap = Array.Empty<ConsoleColor>();
    static string _cachedText = "";

    static void Redraw(List<string> lines, int row, int col, bool forceAll, bool textChanged = true) {
        Console.CursorVisible = false;

        var needed = _startTop + lines.Count;
        while (needed >= Console.BufferHeight && _startTop > 0)
        {
            Console.SetCursorPosition(0, Console.BufferHeight - 1);
            Console.WriteLine();
            _startTop--;
            needed--;
        }

        var width = Math.Max(Console.WindowWidth, 10);

        var fullText = string.Join("\n", lines);
        if (textChanged || fullText != _cachedText)
        {
            _cachedColorMap = BuildColorMap(fullText);
            _cachedText = fullText;
        }
        var colorMap = _cachedColorMap;

        // Bracket matching (#8): find matching bracket at cursor
        int matchPos = -1;
        var cursorOffset = GetOffset(lines, row, col);
        if (cursorOffset > 0 && cursorOffset <= fullText.Length)
        {
            var charBefore = fullText[cursorOffset - 1];
            matchPos = FindMatchingBracket(fullText, cursorOffset - 1);
        }
        if (matchPos < 0 && cursorOffset < fullText.Length)
        {
            matchPos = FindMatchingBracket(fullText, cursorOffset);
            if (matchPos >= 0)
            {
                // highlight both: cursor pos and match pos
                colorMap = (ConsoleColor[])colorMap.Clone();
                colorMap[cursorOffset] = ConsoleColor.White;
                colorMap[matchPos] = ConsoleColor.White;
                matchPos = -2; // signal that we already applied
            }
        }
        if (matchPos >= 0)
        {
            colorMap = (ConsoleColor[])colorMap.Clone();
            colorMap[cursorOffset - 1] = ConsoleColor.White;
            colorMap[matchPos] = ConsoleColor.White;
        }

        var lineOffsets = new int[lines.Count];
        lineOffsets[0] = 0;
        for (int i = 1; i < lines.Count; i++)
            lineOffsets[i] = lineOffsets[i - 1] + lines[i - 1].Length + 1;

        if (forceAll)
        {
            for (int i = 0; i < lines.Count; i++)
                DrawLine(lines, i, width, colorMap, lineOffsets[i]);

            for (int i = lines.Count; i < _prevLineCount; i++)
            {
                var y = _startTop + i;
                if (y < 0 || y >= Console.BufferHeight) break;
                Console.SetCursorPosition(0, y);
                Console.Write(new string(' ', width - 1));
            }
            _prevLineCount = lines.Count;
        }
        else
        {
            DrawLine(lines, row, width, colorMap, lineOffsets[row]);
        }

        var cursorY = Math.Max(0, Math.Min(_startTop + row, Console.BufferHeight - 1));
        var cursorX = Math.Min(PromptLen + col, width - 1);
        Console.SetCursorPosition(cursorX, cursorY);
        Console.CursorVisible = true;
    }

    static void DrawLine(List<string> lines, int i, int width, ConsoleColor[] colorMap, int lineOffset) {
        var y = _startTop + i;
        if (y < 0 || y >= Console.BufferHeight) return;

        Console.SetCursorPosition(0, y);
        var prompt = i == 0 ? "> " : "| ";
        var promptColor = i == 0 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
        Write(prompt, promptColor);

        var maxContent = width - PromptLen - 1;
        var line = lines[i];
        var len = Math.Min(line.Length, maxContent);

        var prevColor = ConsoleColor.Gray;
        Console.ForegroundColor = prevColor;
        for (int c = 0; c < len; c++)
        {
            var charColor = (lineOffset + c < colorMap.Length) ? colorMap[lineOffset + c] : ConsoleColor.Gray;
            if (charColor != prevColor)
            {
                Console.ForegroundColor = charColor;
                prevColor = charColor;
            }
            Console.Write(line[c]);
        }
        Console.ForegroundColor = ConsoleColor.Gray;

        var remaining = width - PromptLen - len - 1;
        if (remaining > 0)
            Console.Write(new string(' ', remaining));
    }

    // ── Syntax highlighting ────────────────────────────────────────────────────
    static ConsoleColor[] BuildColorMap(string text) {
        var map = new ConsoleColor[text.Length];
        Array.Fill(map, ConsoleColor.Gray);

        try
        {
            foreach (var tok in Tokenizer.ToTokens(text))
            {
                if (tok.Is(TokType.Eof)) break;
                var color = TokenColor(tok.Type);
                var start = Math.Max(0, tok.Start);
                var end = Math.Min(text.Length, tok.Finish);
                for (int i = start; i < end; i++)
                    map[i] = color;
            }
        }
        catch { }

        return map;
    }

    static ConsoleColor TokenColor(TokType type) => type switch {
        TokType.If or TokType.Else or TokType.Then => ConsoleColor.Magenta,
        TokType.And or TokType.Or or TokType.Xor or TokType.Not => ConsoleColor.Magenta,
        TokType.In or TokType.Rule or TokType.Default => ConsoleColor.Magenta,
        TokType.Reserved => ConsoleColor.DarkMagenta,
        TokType.True or TokType.False => ConsoleColor.DarkCyan,
        TokType.IntNumber or TokType.RealNumber or TokType.HexOrBinaryNumber => ConsoleColor.Cyan,
        TokType.IpAddress => ConsoleColor.Cyan,
        TokType.CharLiteral => ConsoleColor.DarkYellow,
        TokType.Superscript => ConsoleColor.Cyan,
        TokType.Text => ConsoleColor.Yellow,
        TokType.TextOpenInterpolation or TokType.TextMidInterpolation
            or TokType.TextCloseInterpolation => ConsoleColor.Yellow,
        TokType.TextType or TokType.Int16Type or TokType.Int32Type or TokType.Int64Type
            or TokType.UInt8Type or TokType.UInt16Type or TokType.UInt32Type or TokType.UInt64Type
            or TokType.RealType or TokType.BoolType or TokType.CharType
            or TokType.AnythingType => ConsoleColor.DarkGreen,
        TokType.Plus or TokType.Minus or TokType.Mult or TokType.Div or TokType.DivInt
            or TokType.Rema or TokType.Pow => ConsoleColor.DarkGray,
        TokType.Equal or TokType.NotEqual or TokType.Less or TokType.More
            or TokType.LessOrEqual or TokType.MoreOrEqual => ConsoleColor.DarkGray,
        TokType.BitOr or TokType.BitAnd or TokType.BitXor or TokType.BitInverse
            or TokType.BitShiftLeft or TokType.BitShiftRight => ConsoleColor.DarkGray,
        TokType.NullCoalesce or TokType.SafeAccess or TokType.Question
            or TokType.ForceUnwrap => ConsoleColor.DarkGray,
        TokType.Def => ConsoleColor.White,
        TokType.ParenthObr or TokType.ParenthCbr => ConsoleColor.White,
        TokType.ArrOBr or TokType.ArrCBr => ConsoleColor.White,
        TokType.FiObr or TokType.FiCbr => ConsoleColor.White,
        TokType.Dot or TokType.Sep or TokType.Colon or TokType.TwoDots => ConsoleColor.DarkGray,
        TokType.NotAToken => ConsoleColor.Red,
        _ => ConsoleColor.Gray,
    };

    // ── Bracket matching (#8) ──────────────────────────────────────────────────
    static int GetOffset(List<string> lines, int row, int col) {
        int offset = 0;
        for (int i = 0; i < row; i++)
            offset += lines[i].Length + 1; // +1 for \n
        return offset + col;
    }

    static int FindMatchingBracket(string text, int pos) {
        if (pos < 0 || pos >= text.Length) return -1;

        // Build bracket token list from tokenizer (ignores brackets in strings/comments)
        var brackets = new List<(int Start, int Finish, TokType Type)>();
        try
        {
            foreach (var tok in Tokenizer.ToTokens(text))
            {
                if (tok.Is(TokType.Eof)) break;
                if (tok.Type is TokType.ParenthObr or TokType.ParenthCbr
                    or TokType.ArrOBr or TokType.ArrCBr
                    or TokType.FiObr or TokType.FiCbr)
                    brackets.Add((tok.Start, tok.Finish, tok.Type));
            }
        }
        catch { return -1; }

        // Find the bracket at pos
        int idx = brackets.FindIndex(b => pos >= b.Start && pos < b.Finish);
        if (idx < 0) return -1;

        var (_, _, type) = brackets[idx];
        TokType target;
        int direction;
        switch (type)
        {
            case TokType.ParenthObr: target = TokType.ParenthCbr; direction = 1; break;
            case TokType.ParenthCbr: target = TokType.ParenthObr; direction = -1; break;
            case TokType.ArrOBr:     target = TokType.ArrCBr;     direction = 1; break;
            case TokType.ArrCBr:     target = TokType.ArrOBr;     direction = -1; break;
            case TokType.FiObr:      target = TokType.FiCbr;      direction = 1; break;
            case TokType.FiCbr:      target = TokType.FiObr;      direction = -1; break;
            default: return -1;
        }

        int depth = 0;
        for (int i = idx; i >= 0 && i < brackets.Count; i += direction)
        {
            if (brackets[i].Type == type) depth++;
            else if (brackets[i].Type == target) depth--;
            if (depth == 0) return brackets[i].Start;
        }
        return -1;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    static void LoadFromHistory(List<string> lines, string[] historyEntry, out int row, out int col) {
        lines.Clear();
        lines.AddRange(historyEntry);
        if (lines.Count == 0) lines.Add("");
        row = lines.Count - 1;
        col = lines[row].Length;
    }

    static int FindWordBoundaryLeft(string line, int col) {
        if (col <= 0) return 0;
        var i = col - 1;
        while (i > 0 && !char.IsLetterOrDigit(line[i])) i--;
        while (i > 0 && char.IsLetterOrDigit(line[i - 1])) i--;
        return i;
    }

    static int FindWordBoundaryRight(string line, int col) {
        if (col >= line.Length) return line.Length;
        var i = col;
        while (i < line.Length && !char.IsLetterOrDigit(line[i])) i++;
        while (i < line.Length && char.IsLetterOrDigit(line[i])) i++;
        return i;
    }

    static string ReadSimpleLine(string prompt, ConsoleColor promptColor) {
        Write(prompt, promptColor);
        var buf = new StringBuilder();
        var pos = 0;
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return buf.ToString();
                case ConsoleKey.Backspace:
                    if (pos > 0) { buf.Remove(--pos, 1); RedrawSimple(prompt, buf, pos); }
                    break;
                case ConsoleKey.LeftArrow:
                    if (pos > 0) { pos--; Console.SetCursorPosition(prompt.Length + pos, Console.CursorTop); }
                    break;
                case ConsoleKey.RightArrow:
                    if (pos < buf.Length) { pos++; Console.SetCursorPosition(prompt.Length + pos, Console.CursorTop); }
                    break;
                default:
                    if (key.KeyChar >= ' ') { buf.Insert(pos, key.KeyChar); pos++; RedrawSimple(prompt, buf, pos); }
                    break;
            }
        }
    }

    static void RedrawSimple(string prompt, StringBuilder buf, int pos) {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Math.Max(Console.WindowWidth - 1, 1)));
        Console.SetCursorPosition(0, Console.CursorTop);
        Write(prompt, ConsoleColor.DarkYellow);
        Console.Write(buf);
        Console.SetCursorPosition(prompt.Length + pos, Console.CursorTop);
    }

    // ── Commands ───────────────────────────────────────────────────────────────
    static bool TryHandleCommand(string cmd) {
        var firstLine = cmd.Contains('\n') ? cmd[..cmd.IndexOf('\n')] : cmd;
        var lower = firstLine.Trim().ToLowerInvariant();
        if (lower is "/exit" or "/quit" or "/q")
        {
            SaveHistory();
            Environment.Exit(0);
            return true;
        }
        if (lower is "/help" or "/h" or "/?")
        {
            PrintHelp();
            return true;
        }
        if (lower is "/examples" or "/ex")
        {
            PrintExamples();
            return true;
        }
        if (lower is "/clear" or "/cls")
        {
            Console.Clear();
            return true;
        }
        if (lower is "/reset")
        {
            _sessionDefinitions.Clear();
            WriteDim("  Session cleared.");
            Console.WriteLine();
            return true;
        }
        if (lower.StartsWith("/", StringComparison.Ordinal))
        {
            WriteLineColor($"Unknown command: {lower}. Type /help for commands.", ConsoleColor.DarkRed);
            return true;
        }
        return false;
    }

    // ── Execution ──────────────────────────────────────────────────────────────
    static int ExecuteNonInteractive(string expression) {
        try
        {
            var runtime = Funny.Hardcore.WithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).Build(expression);
            var inputs = runtime.Variables.Where(v => !v.IsOutput).ToList();

            if (inputs.Count > 0)
            {
                Console.Error.WriteLine("Error: expression has unbound inputs: " +
                    string.Join(", ", inputs.Select(i => $"{i.Name}:{i.Type}")));
                return 1;
            }

            runtime.Run();
            var outputs = runtime.Variables.Where(v => v.IsOutput).ToList();

            foreach (var output in outputs)
                Console.WriteLine($"{output.Name}:{output.Type} = {FormatValue(output.Value)}");

            return 0;
        }
        catch (FunnyParseException e)
        {
            Console.Error.WriteLine($"Parse error [FU{e.ErrorCode}]: {e.Message}");
            if (e.Start >= 0 && e.End > 0 && e.End <= expression.Length)
                Console.Error.WriteLine($"  at [{e.Start}..{e.End}]: '{e.Interval.SubString(expression)}'");
            return 1;
        }
        catch (FunnyRuntimeException e)
        {
            Console.Error.WriteLine($"Runtime error: {e.Message}");
            return 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}");
            return 1;
        }
    }

    // Session context (#10): prepend previous definitions to current expression
    static void Execute(string expression) {
        // Build full script: previous definitions + current expression
        var fullScript = _sessionDefinitions.Count > 0
            ? string.Join("\n", _sessionDefinitions) + "\n" + expression
            : expression;

        try
        {
            var runtime = Funny.Hardcore.WithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).Build(fullScript);
            var inputs = runtime.Variables.Where(v => !v.IsOutput).ToList();
            var outputs = runtime.Variables.Where(v => v.IsOutput).ToList();

            if (outputs.Count == 0)
            {
                // No outputs — likely a function definition. Save to session context.
                // Remove previous definition with same function name(s) to avoid duplicates.
                foreach (var uf in runtime.UserFunctions)
                {
                    _sessionDefinitions.RemoveAll(d =>
                        d.StartsWith(uf.Name + "(", StringComparison.Ordinal)
                        || d.TrimStart().StartsWith(uf.Name + "(", StringComparison.Ordinal));
                }
                _sessionDefinitions.Add(expression);
                WriteDim("  (saved to session)");
                if (runtime.UserFunctions.Count > 0)
                {
                    var names = string.Join(", ", runtime.UserFunctions.Select(f => f.Name));
                    WriteDim($"  Functions: {names}");
                }
                Console.WriteLine();
                return;
            }

            if (inputs.Count > 0)
            {
                WriteDim("  Inputs:");
                foreach (var input in inputs)
                    WriteDim($"    {input.Name} : {input.Type}");

                foreach (var input in inputs)
                {
                    var valueStr = ReadSimpleLine($"  {input.Name} = ", ConsoleColor.DarkYellow);
                    if (string.IsNullOrWhiteSpace(valueStr))
                        continue;

                    try
                    {
                        var val = Funny.Calc(valueStr);
                        input.Value = val;
                    }
                    catch (Exception ex)
                    {
                        WriteLineColor($"  Cannot parse value: {ex.Message}", ConsoleColor.Red);
                        Console.WriteLine();
                        return;
                    }
                }
            }

            runtime.Run();

            // Save definitions to session context.
            // Remove previous definitions with same variable/function names.
            foreach (var output in outputs)
            {
                _sessionDefinitions.RemoveAll(d =>
                {
                    var dt = d.TrimStart();
                    return dt.StartsWith(output.Name + " ", StringComparison.Ordinal)
                        || dt.StartsWith(output.Name + "=", StringComparison.Ordinal)
                        || dt.StartsWith(output.Name + "(", StringComparison.Ordinal);
                });
            }
            foreach (var uf in runtime.UserFunctions)
            {
                _sessionDefinitions.RemoveAll(d =>
                    d.TrimStart().StartsWith(uf.Name + "(", StringComparison.Ordinal));
            }
            _sessionDefinitions.Add(expression);

            foreach (var output in outputs)
            {
                Write($"  {output.Name}", ConsoleColor.White);
                WriteDim($" : {output.Type}");
                Write("    = ", ConsoleColor.DarkGray);
                WriteLineColor(FormatValue(output.Value), ConsoleColor.Green);
            }

            Console.WriteLine();
        }
        catch (FunnyParseException e)
        {
            // Adjust error position: subtract session prefix length
            var prefixLen = _sessionDefinitions.Count > 0
                ? string.Join("\n", _sessionDefinitions).Length + 1
                : 0;
            ShowParseError(e, fullScript, prefixLen);
        }
        catch (FunnyRuntimeException e)
        {
            WriteLineColor($"  Runtime error: {e.Message}", ConsoleColor.Red);
            Console.WriteLine();
        }
    }

    static void ShowParseError(FunnyParseException e, string fullScript, int prefixLen) {
        Write(" ERROR ", ConsoleColor.Red);
        WriteDim($"[FU{e.ErrorCode}]");
        Console.WriteLine($"  {e.Message}");

        var adjStart = e.Start - prefixLen;
        var adjEnd = e.End - prefixLen;
        var expression = prefixLen > 0 && prefixLen < fullScript.Length
            ? fullScript[prefixLen..]
            : fullScript;

        if (adjStart >= 0 && adjEnd > 0 && adjEnd <= expression.Length)
        {
            Console.Write("  ");
            if (adjStart > 0)
                Console.Write(expression[..adjStart]);
            Write(expression[adjStart..adjEnd], ConsoleColor.Red);
            if (adjEnd < expression.Length)
                Console.Write(expression[adjEnd..]);
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    // ── Formatting ─────────────────────────────────────────────────────────────
    static string FormatValue(object value) =>
        value switch {
            null => "null",
            bool b => b ? "true" : "false",
            string s => $"'{s}'",
            IFunnyArray arr => arr.ToText(),
            IReadOnlyDictionary<string, object> dict => FormatStruct(dict),
            Array arr => FormatArray(arr),
            _ => value.ToString()
        };

    static string FormatStruct(IReadOnlyDictionary<string, object> dict) {
        var fields = dict.Select(kv => $"{kv.Key} = {FormatValue(kv.Value)}");
        return "{ " + string.Join(", ", fields) + " }";
    }

    static string FormatArray(Array arr) {
        var items = new List<string>();
        foreach (var item in arr)
            items.Add(FormatValue(item));
        return "[" + string.Join(", ", items) + "]";
    }

    // ── Console helpers ────────────────────────────────────────────────────────
    static void Write(string text, ConsoleColor color) {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    static void WriteLineColor(string text, ConsoleColor color) {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    static void WriteDim(string text) {
        WriteLineColor(text, ConsoleColor.DarkGray);
    }

    // ── Help ───────────────────────────────────────────────────────────────────
    static void PrintHelp() {
        Console.WriteLine(@"
  Editor:
    Enter           New line (auto-indent)
    Alt+Enter       Execute expression
    Ctrl+Z / Ctrl+Y  Undo / Redo
    Ctrl+A          Clear all input
    Ctrl+K          Delete to end of line
    Ctrl+L          Clear screen
    Alt+Backspace   Delete word backward
    Alt+Left/Right  Move by word
    Up/Down         Navigate lines / history
    Tab             Insert spaces
    Escape          Clear input
    Ctrl+C          Cancel
    Ctrl+D          Exit (on empty input)

  Session:
    Definitions persist across executions:
      > add(a,b) = a + b          (Alt+Enter)
      > out = add(2, 3)           (Alt+Enter) → 5

  Commands:
    /help, /h       Show this help
    /examples, /ex  Show example expressions
    /clear, /cls    Clear screen
    /reset          Reset session (forget definitions)
    /exit, /q       Exit
");
    }

    static void PrintExamples() {
        var examples = new[] {
            ("Arithmetic",    "out = 2 + 2 * 2"),
            ("Strings",       "out = 'hello world'.reverse()"),
            ("Arrays",        "out = [1,2,3,4].filter(rule it > 2)"),
            ("Map",           "out = [1,2,3].map(rule it * it)"),
            ("Structs",       "out = {name = 'Kate', age = 25}"),
            ("Field access",  "user = {name = 'Kate', age = 25}; out = user.name"),
            ("Functions",     "add(a,b) = a + b\nout = add(2, 3)"),
            ("If-else",       "out = if(1 > 0) 'yes' else 'no'"),
            ("Variables",     "y = x * 2 + 1"),
            ("Math sugar",    "out = 2π + 3²"),
        };

        Console.WriteLine();
        foreach (var (label, code) in examples)
        {
            Write($"  {label,-14}", ConsoleColor.DarkCyan);
            WriteDim($"  {code}");
        }

        Console.WriteLine();
    }
}
