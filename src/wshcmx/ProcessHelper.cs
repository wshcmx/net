using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace wshcmx.Net;

/// <summary>
/// Provides helper methods for executing external processes.
/// </summary>
public static class ProcessHelper
{
    private const int PollIntervalMilliseconds = 50;
    private const int StreamDrainTimeoutMilliseconds = 500;

    /// <summary>
    /// Executes an external process and returns the results.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory in which to run the command.</param>
    /// <param name="timeoutMilliseconds">The maximum time to wait for the process to exit in milliseconds. Use -1 to wait indefinitely.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the execution details.</returns>
    public static ProcessResult Execute(string command, string arguments = "", string? workingDirectory = null, int timeoutMilliseconds = 300000)
    {
        if (timeoutMilliseconds < Timeout.Infinite)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout must be -1 or greater.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var stdoutClosed = new ManualResetEventSlim(false);
        using var stderrClosed = new ManualResetEventSlim(false);
        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                stdoutClosed.Set();
                return;
            }

            outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                stderrClosed.Set();
                return;
            }

            errorBuilder.AppendLine(e.Data);
        };

        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.Now;

        process.Start();
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = WaitForCompletion(process, stdoutClosed, stderrClosed, timeoutMilliseconds);
        if (!completed)
        {
            TryKillProcess(process);
        }

        WaitForStreamToClose(stdoutClosed, process.CancelOutputRead);
        WaitForStreamToClose(stderrClosed, process.CancelErrorRead);

        stopwatch.Stop();
        var exitTime = DateTime.Now;
        var exitCode = 1;
        try
        {
            process.Refresh();
            exitCode = process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"Completed: {completed}, but process has not exited and exit code is unavailable.");
            Console.WriteLine($"Stdout: {outputBuilder}");
            Console.WriteLine($"Stderr: {errorBuilder}");
            if (completed)
            {
                exitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && errorBuilder.Length == 0 ? 0 : exitCode;
            }
        }

        return new ProcessResult(
            exitCode,
            completed,
            outputBuilder.ToString(),
            errorBuilder.ToString(),
            startTime,
            exitTime,
            stopwatch.ElapsedMilliseconds
        );
    }

    private static bool WaitForCompletion(
        Process process,
        ManualResetEventSlim stdoutClosed,
        ManualResetEventSlim stderrClosed,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds == Timeout.Infinite)
        {
            while (!IsCompleted(process, stdoutClosed, stderrClosed))
            {
                Thread.Sleep(PollIntervalMilliseconds);
            }

            return true;
        }

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            if (IsCompleted(process, stdoutClosed, stderrClosed))
            {
                return true;
            }

            Thread.Sleep(PollIntervalMilliseconds);
        }

        return IsCompleted(process, stdoutClosed, stderrClosed);
    }

    private static bool IsCompleted(Process process, ManualResetEventSlim stdoutClosed, ManualResetEventSlim stderrClosed)
    {
        if (TryHasExited(process))
        {
            return true;
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && stdoutClosed.IsSet && stderrClosed.IsSet;
    }

    private static bool TryHasExited(Process process)
    {
        try
        {
            process.Refresh();
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            var killWithTree = typeof(Process).GetMethod(nameof(Process.Kill), [typeof(bool)]);
            if (killWithTree != null)
            {
                killWithTree.Invoke(process, [true]);
                return;
            }

            process.Kill();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void WaitForStreamToClose(ManualResetEventSlim closedSignal, Action cancelRead)
    {
        if (closedSignal.Wait(StreamDrainTimeoutMilliseconds))
        {
            return;
        }

        try
        {
            cancelRead();
        }
        catch (InvalidOperationException)
        {
        }
    }
}
