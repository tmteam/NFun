---
name: nfun-cli
description: Run NFun expressions via the Funny CLI. Use when you need to quickly test an NFun expression, check types, or trace TIC solver behavior. Prefer this over creating ad-hoc .cs test projects.
user-invocable: true
argument-hint: <expression>
allowed-tools: Bash
---

# NFun CLI (Funny CLI)

Run NFun expressions using the console app at:
`src/ConsoleAppExample/ConsoleAppExample.csproj`

## Usage

```bash
# Evaluate an expression
dotnet run --project src/ConsoleAppExample/ConsoleAppExample.csproj --no-build -p:SignAssembly=false -- -e "$ARGUMENTS"

# Evaluate with TIC trace (shows constraint solving steps)
dotnet run --project src/ConsoleAppExample/ConsoleAppExample.csproj --no-build -p:SignAssembly=false -- -t -e "$ARGUMENTS"
```

## Flags

| Flag | Description |
|------|-------------|
| `-e`, `--eval` | Evaluate expression |
| `-s`, `--script` | Run script from file |
| `-t`, `--trace` | Show TIC solver trace output |
| `-h`, `--help` | Show help |

## Important

- Always use `-p:SignAssembly=false` (SNK key is missing)
- Use `--no-build` if you've recently built; otherwise omit it
- If you get assembly load errors, rebuild NFun first:
  `dotnet build src/NFun/NFun.csproj -p:SignAssembly=false -f net6.0`
- Multi-line expressions: use `\r` as line separator in the expression string
- The `-t` flag enables `TraceLog.IsEnabled` which shows all TIC solving phases:
  toposorted, PullConstraints, PushConstraints, Destructed, Finalized

## Examples

```bash
# Simple expression
dotnet run ... -- -e "y = 1 + 2"

# Optional types
dotnet run ... -- -e "y = if(true) 42 else none"

# With trace to debug type inference
dotnet run ... -- -t -e "y = [1,none,3].map(rule it ?? 0)"

# Multi-line
dotnet run ... -- -e "f(x) = x * 2\r y = f(3)"
```
