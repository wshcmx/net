namespace wshcmx.Net;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public class ProcessResult(int exitCode, string standardOutput, string standardError, DateTime startTime, DateTime exitTime, long duration)
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Gets the standard output of the process.
    /// </summary>
    public string StandardOutput { get; } = standardOutput;

    /// <summary>
    /// Gets the standard error of the process.
    /// </summary>
    public string StandardError { get; } = standardError;

    /// <summary>
    /// Gets the time the process started.
    /// </summary>
    public DateTime StartTime { get; } = startTime;

    /// <summary>
    /// Gets the time the process exited.
    /// </summary>
    public DateTime ExitTime { get; } = exitTime;

    /// <summary>
    /// Gets the total duration of the process execution in milliseconds.
    /// </summary>
    public long Duration { get; } = duration;

    /// <summary>
    /// Gets a value indicating whether the process exited successfully (ExitCode == 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
