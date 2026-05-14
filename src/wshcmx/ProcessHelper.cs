using System.Diagnostics;
using System.Text;

namespace wshcmx.Net;

/// <summary>
/// Provides helper methods for executing external processes.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Executes an external process and returns the results.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory in which to run the command.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the execution details.</returns>
    public static ProcessResult Execute(string command, string arguments = "", string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.Now;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
        stopwatch.Stop();
        var exitTime = DateTime.Now;

        return new ProcessResult(
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString(),
            startTime,
            exitTime,
            stopwatch.Elapsed.Milliseconds
        );
    }
}
