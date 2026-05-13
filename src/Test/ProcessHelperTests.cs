using wshcmx;
using Xunit;

namespace Test;

public class ProcessHelperTests
{
    [Fact]
    public void Execute_SuccessfulCommand_ReturnsSuccessResult()
    {
        // Arrange
        string command = "cmd.exe";
        string arguments = "/c echo Hello World";

        // Act
        var result = ProcessHelper.Execute(command, arguments);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.IsSuccess);
        Assert.Contains("Hello World", result.StandardOutput);
        Assert.Empty(result.StandardError);
        Assert.True(result.Duration > TimeSpan.Zero);
    }

    [Fact]
    public void Execute_FailingCommand_ReturnsFailureResult()
    {
        // Arrange
        string command = "cmd.exe";
        string arguments = "/c exit 1";

        // Act
        var result = ProcessHelper.Execute(command, arguments);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Execute_CommandWithStderr_CapturesStderr()
    {
        // Arrange
        string command = "cmd.exe";
        string arguments = "/c echo error message >&2";

        // Act
        var result = ProcessHelper.Execute(command, arguments);

        // Assert
        Assert.Contains("error message", result.StandardError);
    }

    [Fact]
    public void Execute_PopulatesTimestampsAndDuration()
    {
        // Arrange
        string command = "cmd.exe";
        string arguments = "/c ping 127.0.0.1 -n 2";

        // Act
        var result = ProcessHelper.Execute(command, arguments);

        // Assert
        Assert.True(result.StartTime <= result.ExitTime);
        Assert.True(result.Duration >= TimeSpan.FromSeconds(1));
    }
}
