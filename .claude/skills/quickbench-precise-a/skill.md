# QuickBench Precise (V1 Set)

Run the V1 benchmark set with Precise accuracy (60s measurement window).

## CRITICAL: Clean Build Protocol

**ALWAYS clean+rebuild before benchmarking.** Stale binaries give wrong results.

```bash
dotnet clean src/NFun/NFun.csproj -c Release -q 2>/dev/null
dotnet clean src/Benchmarks/NFun.Benchmarks/NFun.Benchmarks.csproj -c Release -q 2>/dev/null
dotnet build src/Benchmarks/NFun.Benchmarks/NFun.Benchmarks.csproj -p:SignAssembly=false -c Release
# Verify: grep for "Build succeeded" and "0 Error(s)"
```

## Run Benchmark

```bash
dotnet test src/Benchmarks/NFun.Benchmarks/NFun.Benchmarks.csproj -p:SignAssembly=false -c Release --no-build --filter "Precise"
```

Takes ~140s (two 70s runs: V1 + V2).

## Read Results

```bash
FILE=$(ls -t src/Benchmarks/NFun.Benchmarks/bin/Release/net6.0/bench_v1_*.json | head -1)
python3 -c "
import json
with open('$FILE') as f: d = json.load(f)
bl = d['Baseline']['MeanUs']
print(f'Baseline: {bl:.1f}μs')
for name in ['Simple', 'Medium', 'Complex']:
    s = {x['Tags'][1]: x for x in d['Slots'] if x['Tags'][0] == name}
    p = s.get('Parse',{}).get('MeanUs',0)
    b = s.get('Build',{}).get('MeanUs',0)
    a = s.get('Build',{}).get('AllocKb',0)
    print(f'{name:10} Parse={p:.1f}μs Build={b:.1f}μs TIC+Asm={b-p:.1f}μs Alloc={a:.1f}kb')
"
```

## A/B Comparison

```bash
python3 -c "
import json
with open('FILE_A') as f: a = json.load(f)
with open('FILE_B') as f: b = json.load(f)
for name in ['Simple', 'Medium', 'Complex']:
    sa = {x['Tags'][1]: x for x in a['Slots'] if x['Tags'][0] == name}
    sb = {x['Tags'][1]: x for x in b['Slots'] if x['Tags'][0] == name}
    ba, bb = sa.get('Build',{}).get('MeanUs',0), sb.get('Build',{}).get('MeanUs',0)
    d = (1-bb/ba)*100 if ba>0 else 0
    print(f'{name:10} A={ba:.1f}μs B={bb:.1f}μs  Δ={d:+.1f}%')
"
```

**Never compare without clean builds on both sides.** Use `rm -rf src/NFun/obj src/NFun/bin src/Benchmarks/NFun.Benchmarks/obj src/Benchmarks/NFun.Benchmarks/bin` then rebuild. `dotnet clean` is NOT sufficient.

**Save results before rm -rf**: `cp bench_v1_*.json /tmp/` — rm -rf deletes the JSON.

## Baseline Validation

**Expected baseline on MacBook M4: 92.3 ± 0.5μs.** If baseline is in this corridor, results are directly comparable across runs without re-measuring. If outside: CPU is throttled (hot) or in different power mode. Wait/cool down and retry.

## V1 Categories

| Category | Weight | SPS? | Content |
|----------|--------|------|---------|
| Simple | 64 | YES | `y = 2*x+1`, `max(a,b)`, if-else, typed I/O |
| Medium | 8 | NO | arrays, map/filter/fold, user functions, structs |
| Complex | 1 | NO | bubble sort, recursion, struct pipelines |

## Current baseline (2026-04-24, branch oops-bottom-type, IsSimpleBody gate)

| | SPS OFF | SPS ON | Δ |
|---|---|---|---|
| Simple Build | 13.21μs | 12.43μs | **+5.9%** |
| Medium Build | 51.31μs | 51.75μs | -0.9% (noise) |
| Complex Build | 159.05μs | 159.36μs | -0.2% (noise) |
| Weighted | 19.38μs | 18.75μs | **+3.2%** |
