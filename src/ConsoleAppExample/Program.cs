using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Exceptions;
using NFun.Runtime.Arrays;
using NFun.Tic;

namespace NFun.ConsoleApp;

class Program {
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
            var script = System.IO.File.ReadAllText(argList[1]);
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

        return 0;
    }

    static void PrintWelcome() {
        Write("NFun", ConsoleColor.Cyan);
        Console.Write(" Playground  ");
        WriteDim("Type /help for commands, /ex for examples");
        Console.WriteLine();
    }

    static string ReadExpression() {
        Write("> ", ConsoleColor.DarkYellow);
        var first = Console.ReadLine();
        if (first == null)
            return null;
        if (first.Trim().Length == 0)
            return "";
        if (first.TrimStart().StartsWith("/"))
            return first.Trim();

        var sb = new StringBuilder(first);
        while (true)
        {
            Write("| ", ConsoleColor.DarkGray);
            var line = Console.ReadLine();
            if (line == null || line == "")
                break;
            sb.AppendLine();
            sb.Append(line);
        }

        return sb.ToString();
    }

    static bool TryHandleCommand(string cmd) {
        var lower = cmd.ToLowerInvariant();
        if (lower is "/exit" or "/quit" or "/q")
        {
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

        if (cmd.StartsWith("/"))
        {
            WriteLineColor($"Unknown command: {cmd}. Type /help for commands.", ConsoleColor.DarkRed);
            return true;
        }

        return false;
    }

    static int ExecuteNonInteractive(string expression) {
        try
        {
            var runtime = Funny.Hardcore.Build(expression);
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

    static void Execute(string expression) {
        try
        {
            var runtime = Funny.Hardcore.Build(expression);
            var inputs = runtime.Variables.Where(v => !v.IsOutput).ToList();
            var outputs = runtime.Variables.Where(v => v.IsOutput).ToList();

            if (outputs.Count == 0)
            {
                WriteDim("  (no output variables)");
                if (runtime.UserFunctions.Count > 0)
                {
                    var names = string.Join(", ", runtime.UserFunctions.Select(f => f.Name));
                    WriteDim($"  Functions defined: {names}");
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
                    Write($"  {input.Name} = ", ConsoleColor.DarkYellow);
                    var valueStr = Console.ReadLine();
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
            ShowParseError(e, expression);
        }
        catch (FunnyRuntimeException e)
        {
            WriteLineColor($"  Runtime error: {e.Message}", ConsoleColor.Red);
            Console.WriteLine();
        }
    }

    static void ShowParseError(FunnyParseException e, string expression) {
        Write(" ERROR ", ConsoleColor.Red);
        WriteDim($"[FU{e.ErrorCode}]");
        Console.WriteLine($"  {e.Message}");

        if (e.Start >= 0 && e.End > 0 && e.End <= expression.Length)
        {
            Console.Write("  ");
            if (e.Start > 0)
                Console.Write(expression[..e.Start]);
            WriteColor(e.Interval.SubString(expression), ConsoleColor.Red);
            if (e.End < expression.Length)
                Console.Write(expression[e.End..]);
            Console.WriteLine();
        }

        Console.WriteLine();
    }

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

    static void Write(string text, ConsoleColor color) {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    static void WriteColor(string text, ConsoleColor color) {
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

    static void PrintHelp() {
        Console.WriteLine(@"
  Usage:
    Type an NFun expression, press Enter, then Enter again to run.
    Multi-line scripts: keep typing, empty line executes.
    If the script has input variables, you'll be prompted for values.

  Commands:
    /help, /h       Show this help
    /examples, /ex  Show example expressions
    /exit, /q       Exit

  NFun types:
    bool, int, real, text, int[], {field:type}

  NFun features:
    rule          Anonymous function:  rule it * 2
    if/else       Ternary:  if(x > 0) x else -x
    . (dot)       Field access:  user.name
    [i]           Array indexing
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
            ("Complex",       "items = [{name='tea', price=5}, {name='coffee', price=8}]\nout = items.filter(rule it.price > 6).map(rule it.name)"),
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
