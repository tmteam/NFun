using System;
using System.Collections.Generic;
using System.Threading;

namespace NFun.VM;

/// <summary>
/// A fiber is an independent execution context with its own VM state.
/// Used for cooperative concurrency: delayed(), parallel(), timer().
/// </summary>
public class Fiber {
    public int Id { get; }
    public FiberStatus Status { get; internal set; }

    internal readonly CompiledProgram Program;
    internal readonly FunValue[] Locals;
    internal DateTime? ResumeAfter;

    public Fiber(int id, CompiledProgram program, FunValue[] locals) {
        Id = id;
        Program = program;
        Locals = locals;
        Status = FiberStatus.Runnable;
    }

    /// <summary>Get result from output variable after fiber completes.</summary>
    public object GetOutput(string name, VariableSlot[] variables) {
        foreach (var v in variables) {
            if (string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
                return Locals[v.Slot].Box(v.Type);
        }
        throw new KeyNotFoundException($"Variable '{name}' not found");
    }
}

public enum FiberStatus {
    Runnable,
    Running,
    Suspended,  // Waiting for delay
    Done,
    Error
}

/// <summary>
/// Cooperative scheduler for fibers. Round-robin, yield after N instructions.
/// All fibers share the same CompiledProgram (read-only).
/// </summary>
public class Scheduler {
    private readonly CompiledProgram _program;
    private readonly Fiber[] _fibers;
    private int _nextFiberId;

    public Scheduler(CompiledProgram program, int maxFibers = 64) {
        _program = program;
        _fibers = new Fiber[maxFibers];
    }

    /// <summary>Create a new fiber with the given locals.</summary>
    public Fiber SpawnFiber(FunValue[] locals) {
        var id = _nextFiberId++;
        var fiber = new Fiber(id, _program, locals);
        _fibers[id] = fiber;
        return fiber;
    }

    /// <summary>
    /// Run all fibers cooperatively until all are done or suspended.
    /// Each fiber gets a time slice of maxOpsPerSlice instructions.
    /// </summary>
    public void RunAll(int maxOpsPerSlice = 1024, int maxTotalOps = 10_000_000) {
        int totalOps = 0;

        while (totalOps < maxTotalOps) {
            bool anyActive = false;

            for (int i = 0; i < _nextFiberId; i++) {
                var fiber = _fibers[i];
                if (fiber == null) continue;

                // Check delayed fibers
                if (fiber.Status == FiberStatus.Suspended && fiber.ResumeAfter.HasValue) {
                    if (DateTime.UtcNow >= fiber.ResumeAfter.Value)
                        fiber.Status = FiberStatus.Runnable;
                    else {
                        anyActive = true;
                        continue;
                    }
                }

                if (fiber.Status != FiberStatus.Runnable) continue;

                anyActive = true;
                fiber.Status = FiberStatus.Running;

                try {
                    VirtualMachine.Execute(fiber.Program, fiber.Locals, maxOpsPerSlice);
                    fiber.Status = FiberStatus.Done;
                }
                catch (Exception ex) when (ex.Message == "Operation budget exceeded") {
                    // Fiber yielded — still runnable
                    fiber.Status = FiberStatus.Runnable;
                }
                catch (Exception) {
                    fiber.Status = FiberStatus.Error;
                }

                totalOps += maxOpsPerSlice;
            }

            if (!anyActive) break;

            // Small sleep to avoid busy-wait when fibers are delayed
            bool allSuspended = true;
            for (int i = 0; i < _nextFiberId; i++) {
                if (_fibers[i]?.Status == FiberStatus.Runnable) { allSuspended = false; break; }
            }
            if (allSuspended) Thread.Sleep(1);
        }
    }
}
