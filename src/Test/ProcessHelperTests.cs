using System.Runtime.InteropServices;
using wshcmx.Net;

namespace Test;

public class ProcessHelperTests
{
    private static (string Command, string Arguments) Shell(string script)
    {
        if (OperatingSystem.IsWindows()) return ("cmd.exe", $"/c {script}");
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) return ("/bin/sh", $"-c \"{script}\"");
        throw new PlatformNotSupportedException("Unsupported operating system.");
    }

    private static (string Command, string Arguments) SleepCommand(int seconds)
    {
        return OperatingSystem.IsWindows()
            ? ("cmd.exe", $"/c ping 127.0.0.1 -n {seconds + 1} >nul")
            : ("/bin/sh", $"-c \"sleep {seconds}\"");
    }

    private static (string Command, string Arguments) CurrentDirectoryCommand()
    {
        return OperatingSystem.IsWindows()
            ? ("cmd.exe", "/c cd")
            : ("/bin/sh", "-c pwd");
    }

    [Fact]
    public void Execute_SuccessfulCommand_ReturnsSuccessResult()
    {
        var (command, arguments) = Shell("echo Hello World");

        var result = ProcessHelper.Execute(command, arguments);

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.IsSuccess);
        Assert.Contains("Hello World", result.StandardOutput);
        Assert.Empty(result.StandardError);
        Assert.True(result.Duration >= 0);
    }

    [Fact]
    public void Execute_FailingCommand_ReturnsFailureResult()
    {
        var (command, arguments) = Shell("exit 1");

        var result = ProcessHelper.Execute(command, arguments);

        Assert.Equal(1, result.ExitCode);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Execute_CommandWithStderr_CapturesStderr()
    {
        var (command, arguments) = Shell("echo error message >&2");

        var result = ProcessHelper.Execute(command, arguments);

        Assert.Contains("error message", result.StandardError);
    }

    [Fact]
    public void Execute_PopulatesTimestampsAndDuration()
    {
        var (command, arguments) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ("cmd.exe", "/c ping 127.0.0.1 -n 3 >nul")
            : ("/bin/sh", "-c \"sleep 2\"");

        var result = ProcessHelper.Execute(command, arguments);

        Assert.True(result.StartTime <= result.ExitTime);
        Assert.True(result.Duration >= 1500);
    }

    [Fact]
    public void Execute_InvalidTimeout_ThrowsArgumentOutOfRangeException()
    {
        var (command, arguments) = Shell("echo Hello World");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProcessHelper.Execute(command, arguments, timeoutMilliseconds: -2));

        Assert.Equal("timeoutMilliseconds", exception.ParamName);
    }

    [Fact]
    public void Execute_TimedOutCommand_ReturnsIncompleteFailureResult()
    {
        var (command, arguments) = SleepCommand(2);

        var result = ProcessHelper.Execute(command, arguments, timeoutMilliseconds: 100);

        Assert.False(result.Completed);
        Assert.False(result.IsSuccess);
        Assert.True(result.ExitCode != 0);
    }

    [Fact]
    public void Execute_WithWorkingDirectory_RunsProcessInSpecifiedDirectory()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"{nameof(ProcessHelperTests)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var (command, arguments) = CurrentDirectoryCommand();

            var result = ProcessHelper.Execute(command, arguments, workingDirectory: workingDirectory);

            var actualDirectory = result.StandardOutput.Trim();
            Assert.True(result.Completed);
            Assert.True(result.IsSuccess);
            Assert.True(
                string.Equals(
                    Path.GetFullPath(workingDirectory),
                    Path.GetFullPath(actualDirectory),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal),
                $"Expected working directory '{workingDirectory}' but got '{actualDirectory}'.");
        }
        finally
        {
            Directory.Delete(workingDirectory);
        }
    }

    [Fact]
    public void Execute_InfiniteTimeout_WaitsForCompletion()
    {
        var (command, arguments) = SleepCommand(1);

        var result = ProcessHelper.Execute(command, arguments, timeoutMilliseconds: Timeout.Infinite);

        Assert.True(result.Completed);
        Assert.True(result.IsSuccess);
        Assert.True(result.Duration >= 500);
    }
}
