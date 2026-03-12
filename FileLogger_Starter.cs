using System;
using System.IO;

namespace MemoryLab.Project1;

// =============================================================================
//  PROJECT 1 — Resource-Safe File Logger (Starter)
// =============================================================================
//  GOAL: Build a logging service that writes to multiple files. Along the way,
//        you'll discover Stack vs Heap, IDisposable, using statements, and
//        what happens when you forget to release resources.
//
//  HOW TO RUN: dotnet run
//  INSTRUCTIONS: Complete each TODO in order. Run after each step to observe.
// =============================================================================

// ---------------------------------------------------------------------------
//  EXERCISE 1: Stack vs Heap — Where does your data live?
// ---------------------------------------------------------------------------

class MemoryBasics
{
    public static void Explore()
    {
        Console.WriteLine("=== EXERCISE 1: Stack vs Heap ===\n");

        // Value types live on the Stack (fast, automatic cleanup)
        int logCount = 0;
        double timestamp = 1234.56;
        bool isActive = true;

        // TODO 1: Print the size of each value type using sizeof()
        //         Example: Console.WriteLine($"int size: {sizeof(int)} bytes");
        //         Do this for int, double, and bool.
        //         QUESTION: Why does the runtime know the exact size at compile time?

        Console.WriteLine($"int size: {sizeof(int)} bytes");
        Console.WriteLine($"double size: {sizeof(double)} bytes");
        Console.WriteLine($"bool size: {sizeof(bool)} bytes");

        // Reference types live on the Heap (managed by Garbage Collector)
        string message = "Hello, Logger!";
        int[] errorCodes = new int[] { 404, 500, 503 };

        // TODO 2: Use GC.GetGeneration() to check which GC generation these objects are in.
        //         Example: Console.WriteLine($"message is in Gen {GC.GetGeneration(message)}");
        //         Do this for 'message' and 'errorCodes'.
        //         QUESTION: Why do they both start in Generation 0?
        Console.WriteLine($"Message is in Gen {GC.GetGeneration(message)}");
        Console.WriteLine($"ErroCodes is in Gen {GC.GetGeneration(errorCodes)}");

        // TODO 3: Force a garbage collection and check generations again.
        //         Call GC.Collect() and then re-check with GC.GetGeneration().
        //         QUESTION: Did the generation number change? Why?

        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"\n  After GC.Collect():");
        Console.WriteLine($"'message us now in Gen {GC.GetGeneration(message)}'");
        Console.WriteLine($"ErroCodes is in Gen {GC.GetGeneration(errorCodes)}");

        Console.WriteLine();
    }
}

// ---------------------------------------------------------------------------
//  EXERCISE 2: The Problem — Forgetting to release resources
// ---------------------------------------------------------------------------

class LeakyLogger
{
    // This logger opens a file but has NO cleanup mechanism.
    private StreamWriter? _writer;
    private string _path;

    public LeakyLogger(string path)
    {
        _path = path;
        _writer = new StreamWriter(path, append: true);
        Console.WriteLine($"  [LeakyLogger] Opened file handle for: {path}");
    }

    public void Log(string message)
    {
        _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    // TODO 4: Try running LeakDemo() below. What happens when you try to
    //         open the SAME file with a second logger while the first is
    //         still alive? (Hint: you'll get an IOException on Windows,
    //         or observe unflushed data on any OS.)
    //
    //         QUESTION: The GC will *eventually* clean this up via a
    //         finalizer on StreamWriter. Why is "eventually" not good enough?
    //TODO 4 ANSWER:
    // The file handle stays open until the GC finalizer runs, which could be seconds, minutes or never
}

class LeakDemo
{
    public static void Run()
    {
        Console.WriteLine("=== EXERCISE 2: Resource Leak Demo ===\n");

        var logger = new LeakyLogger("leak_test.log");
        logger.Log("First message");
        logger.Log("Second message");

        // TODO 5: Uncomment the following lines and run. Observe what happens.
        //
        Console.WriteLine("  Checking file content BEFORE dispose...");
        try
        {
            //File exists but data my not be flushed yet
            var info = new FileInfo("Leak_test.log");
            Console.WriteLine($"File size: {info.Length} bytes");
            if(info.Length == 0)
                 Console.WriteLine("  Data is buffered in memory - NOT on disk yet!");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($" Error accessing file: {ex.Message}");
           
        }      

        // The logger goes out of scope here, but the file handle is NOT released.
        // The GC *might* clean it up later, or it might not for a long time.



        Console.WriteLine("  [!] LeakyLogger went out of scope — file handle is still open.");
        Console.WriteLine("      In production, this causes file locks and data loss.\n");
    }
}

// ---------------------------------------------------------------------------
//  EXERCISE 3: The Solution — Implementing IDisposable
// ---------------------------------------------------------------------------

// TODO 6: Complete this class by implementing the IDisposable pattern.
//         Follow the steps marked below.

class SafeFileLogger : IDisposable
{
    private StreamWriter? _writer;
    private readonly string _filePath;
    private bool _disposed = false;  // Prevents double-dispose

    public SafeFileLogger(string filePath)
    {
        _filePath = filePath;
        _writer = new StreamWriter(filePath, append: true) { AutoFlush = false };
        Console.WriteLine($"  [SafeLogger] Opened: {filePath}");
    }

    public void Log(string level, string message)
    {
        // TODO 7: Before writing, check if this object has been disposed.
        //         If _disposed is true, throw an ObjectDisposedException.
        //         This prevents usage after cleanup — a common production bug.
        //
        // if (_disposed) throw new ObjectDisposedException(nameof(SafeFileLogger));

        _writer?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
    }

    public void Flush()
    {
        if (!_disposed)
            _writer?.Flush();
    }

    // TODO 8: Implement the Dispose() method.
    //         This is called by the 'using' statement (or manually).
    //
    // public void Dispose()
    // {
    //     if (!_disposed)
    //     {
    //         _writer?.Flush();
    //         _writer?.Dispose();     // Release the file handle
    //         _writer = null;
    //         _disposed = true;
    //         Console.WriteLine($"  [SafeLogger] Disposed: {_filePath}");
    //
    //         GC.SuppressFinalize(this);  // No need for finalizer now
    //     }
    // }

    // TODO 9: Add a finalizer (destructor) as a safety net.
    //         This runs IF someone forgets to call Dispose().
    //         WARNING: Finalizers are slow and non-deterministic.
    //
    // ~SafeFileLogger()
    // {
    //     Console.WriteLine($"  [!] FINALIZER ran for {_filePath} — someone forgot to Dispose!");
    //     Dispose();
    // }

    // Stub to make the code compile before TODO 8 is done:
    public void Dispose() { /* Replace this with TODO 8 */ }
}

// ---------------------------------------------------------------------------
//  EXERCISE 4: Using the 'using' statement — Automatic cleanup
// ---------------------------------------------------------------------------

class UsingDemo
{
    public static void Run()
    {
        Console.WriteLine("=== EXERCISE 4: 'using' Statement Demo ===\n");

        // TODO 10: Wrap SafeFileLogger in a 'using' block.
        //          The 'using' statement automatically calls Dispose() at the end,
        //          even if an exception is thrown inside the block.
        //
        //          Replace the manual approach below with:
        //
        //   using (var logger = new SafeFileLogger("safe_app.log"))
        //   {
        //       logger.Log("INFO", "Application started");
        //       logger.Log("WARN", "Low disk space");
        //       logger.Log("ERROR", "Connection timeout");
        //   }  // <-- Dispose() is called automatically here
        //
        //   Console.WriteLine("  Logger disposed automatically by 'using' block.\n");

        // Current manual approach (replace with 'using' above):
        var logger = new SafeFileLogger("safe_app.log");
        logger.Log("INFO", "Application started");
        logger.Log("WARN", "Low disk space");
        logger.Log("ERROR", "Connection timeout");
        // Oops — no Dispose() call! The file handle leaks.

        Console.WriteLine("  [!] Manual approach — easy to forget Dispose().\n");

        // TODO 11 (BONUS): C# 8+ has a simpler 'using declaration' syntax:
        //
        //   using var logger2 = new SafeFileLogger("safe_app2.log");
        //   logger2.Log("INFO", "This is cleaner syntax");
        //   // Dispose() happens at the end of the enclosing scope
        //
        //   QUESTION: What's the difference between 'using' block and 'using' declaration?
        //   When would you prefer one over the other?
    }
}

// ---------------------------------------------------------------------------
//  EXERCISE 5: Monitoring memory in real time
// ---------------------------------------------------------------------------

class MemoryMonitor
{
    public static void Run()
    {
        Console.WriteLine("=== EXERCISE 5: Memory Monitoring ===\n");

        // TODO 12: Record memory BEFORE allocating objects.
        //   long memBefore = GC.GetTotalMemory(forceFullCollection: true);
        //   Console.WriteLine($"  Memory before: {memBefore:N0} bytes");

        // TODO 13: Create several large objects (e.g., big string arrays)
        //   var logs = new string[10_000];
        //   for (int i = 0; i < logs.Length; i++)
        //       logs[i] = $"Log entry #{i}: {new string('X', 200)}";

        // TODO 14: Record memory AFTER and compute the difference.
        //   long memAfter = GC.GetTotalMemory(false);
        //   Console.WriteLine($"  Memory after:  {memAfter:N0} bytes");
        //   Console.WriteLine($"  Difference:    {memAfter - memBefore:N0} bytes");
        //   Console.WriteLine($"  GC Gen0 collections: {GC.CollectionCount(0)}");
        //   Console.WriteLine($"  GC Gen1 collections: {GC.CollectionCount(1)}");
        //   Console.WriteLine($"  GC Gen2 collections: {GC.CollectionCount(2)}");

        // TODO 15: Set logs = null and force a collection. Observe memory drop.
        //   logs = null;
        //   long memFinal = GC.GetTotalMemory(forceFullCollection: true);
        //   Console.WriteLine($"\n  After cleanup: {memFinal:N0} bytes");
        //   Console.WriteLine($"  Reclaimed:     {memAfter - memFinal:N0} bytes");

        Console.WriteLine("  (Uncomment the TODOs above to see memory tracking in action)\n");
    }
}

// ---------------------------------------------------------------------------
//  MAIN — Run all exercises
// ---------------------------------------------------------------------------

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║  PROJECT 1: Resource-Safe File Logger           ║");
        Console.WriteLine("║  .NET Memory Management — Fundamentals          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝\n");

        MemoryBasics.Explore();
        LeakDemo.Run();
        UsingDemo.Run();
        MemoryMonitor.Run();

        Console.WriteLine("════════════════════════════════════════════════════");
        Console.WriteLine("  Done! Complete each TODO, then compare with the");
        Console.WriteLine("  Solution file to check your understanding.");
        Console.WriteLine("════════════════════════════════════════════════════");

        // Cleanup test files
        try { File.Delete("leak_test.log"); } catch { }
        try { File.Delete("safe_app.log"); } catch { }
    }
}
