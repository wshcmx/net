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
}
