using System.IO;

namespace Reqnroll.IdeSupport.Common.Tests.Diagnostics;

// Isolates REQNROLLVS_DEBUG per test: some dev machines have it set ambiently (e.g. from the
// legacy Reqnroll.VisualStudio extension), which would otherwise leak into unrelated assertions.
public class SynchronousFileLoggerTests : IDisposable
{
    private readonly string? _originalReqnrollVsDebug;

    public SynchronousFileLoggerTests()
    {
        _originalReqnrollVsDebug = Environment.GetEnvironmentVariable("REQNROLLVS_DEBUG");
        Environment.SetEnvironmentVariable("REQNROLLVS_DEBUG", null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("REQNROLLVS_DEBUG", _originalReqnrollVsDebug);
    }

    [Fact]
    public void Default_level_is_Warning()
    {
        var logger = new SynchronousFileLogger("test", $"default-{Guid.NewGuid():N}");
        try
        {
            logger.Level.Should().Be(TraceLevel.Warning);
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Theory]
    [InlineData(TraceLevel.Off)]
    [InlineData(TraceLevel.Error)]
    [InlineData(TraceLevel.Warning)]
    [InlineData(TraceLevel.Info)]
    [InlineData(TraceLevel.Verbose)]
    public void Explicit_level_is_honored(TraceLevel level)
    {
        var logger = new SynchronousFileLogger("test", $"explicit-{Guid.NewGuid():N}", level);
        try
        {
            logger.Level.Should().Be(level);
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Fact]
    public void Messages_above_the_configured_level_are_dropped()
    {
        var logger = new SynchronousFileLogger("test", $"filter-{Guid.NewGuid():N}", TraceLevel.Warning);
        try
        {
            logger.Log(new LogMessage(TraceLevel.Info, "should be dropped",
                nameof(Messages_above_the_configured_level_are_dropped)));

            File.Exists(logger.LogFilePath).Should().BeFalse(
                "Info is below the Warning threshold and should never be written");
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Fact]
    public void Messages_at_or_below_the_configured_level_are_written()
    {
        var logger = new SynchronousFileLogger("test", $"filter-{Guid.NewGuid():N}", TraceLevel.Warning);
        try
        {
            logger.Log(new LogMessage(TraceLevel.Warning, "should be written",
                nameof(Messages_at_or_below_the_configured_level_are_written)));

            File.ReadAllText(logger.LogFilePath).Should().Contain("should be written");
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void REQNROLLVS_DEBUG_truthy_value_forces_Verbose(string envValue)
    {
        Environment.SetEnvironmentVariable("REQNROLLVS_DEBUG", envValue);
        var logger = new SynchronousFileLogger("test", $"env-truthy-{Guid.NewGuid():N}", TraceLevel.Warning);
        try
        {
            logger.Level.Should().Be(TraceLevel.Verbose);
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Fact]
    public void REQNROLLVS_DEBUG_TraceLevel_name_overrides_configured_level()
    {
        Environment.SetEnvironmentVariable("REQNROLLVS_DEBUG", "Error");
        var logger = new SynchronousFileLogger("test", $"env-named-{Guid.NewGuid():N}", TraceLevel.Verbose);
        try
        {
            logger.Level.Should().Be(TraceLevel.Error);
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Fact]
    public void REQNROLLVS_DEBUG_unparsable_value_leaves_configured_level_unchanged()
    {
        Environment.SetEnvironmentVariable("REQNROLLVS_DEBUG", "not-a-trace-level");
        var logger = new SynchronousFileLogger("test", $"env-invalid-{Guid.NewGuid():N}", TraceLevel.Warning);
        try
        {
            logger.Level.Should().Be(TraceLevel.Warning);
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    [Fact]
    public void REQNROLLVS_DEBUG_unset_leaves_configured_level_unchanged()
    {
        var logger = new SynchronousFileLogger("test", $"env-unset-{Guid.NewGuid():N}", TraceLevel.Info);
        try
        {
            logger.Level.Should().Be(TraceLevel.Info);
        }
        finally
        {
            DeleteLogFile(logger);
        }
    }

    private static void DeleteLogFile(SynchronousFileLogger logger)
    {
        try { File.Delete(logger.LogFilePath); } catch { /* best-effort cleanup */ }
    }
}
