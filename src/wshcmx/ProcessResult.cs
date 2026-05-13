using System;

namespace wshcmx;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the standard output of the process.
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// Gets the standard error of the process.
    /// </summary>
    public string StandardError { get; }

    /// <summary>
    /// Gets the time the process started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the time the process exited.
    /// </summary>
    public DateTime ExitTime { get; }

    /// <summary>
    /// Gets the total duration of the process execution.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets a value indicating whether the process exited successfully (ExitCode == 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    public ProcessResult(int exitCode, string standardOutput, string standardError, DateTime startTime, DateTime exitTime, TimeSpan duration)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        StartTime = startTime;
        ExitTime = exitTime;
        Duration = duration;
    }
}
