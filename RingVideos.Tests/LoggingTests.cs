using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace RingVideos.Tests;

public class LoggingTests
{
    [Fact]
    public void Logger_CanBeConfigured()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        Assert.NotNull(logger);
    }

    [Fact]
    public void Logger_CanLogInformationMessages()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Information("Test message");

        Assert.Single(events);
        Assert.Equal(LogEventLevel.Information, events[0].Level);
    }

    [Fact]
    public void Logger_CanLogWarningMessages()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Warning("Warning message");

        Assert.Single(events);
        Assert.Equal(LogEventLevel.Warning, events[0].Level);
    }

    [Fact]
    public void Logger_CanLogErrorMessages()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Error("Error message");

        Assert.Single(events);
        Assert.Equal(LogEventLevel.Error, events[0].Level);
    }

    [Fact]
    public void Logger_CanLogDebugMessages()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Debug("Debug message");

        Assert.Single(events);
        Assert.Equal(LogEventLevel.Debug, events[0].Level);
    }

    [Fact]
    public void Logger_WithProperties()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Information("User logged in with {UserId}", 123);

        Assert.Single(events);
        Assert.Contains("UserId", events[0].Properties.Keys);
    }

    [Fact]
    public void Logger_MultipleMessages()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Information("First message");
        logger.Information("Second message");
        logger.Warning("Third message");

        Assert.Equal(3, events.Count);
        Assert.Equal(LogEventLevel.Information, events[0].Level);
        Assert.Equal(LogEventLevel.Information, events[1].Level);
        Assert.Equal(LogEventLevel.Warning, events[2].Level);
    }

    [Fact]
    public void Logger_WithException()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        var ex = new InvalidOperationException("Test exception");
        logger.Error(ex, "An error occurred");

        Assert.Single(events);
        Assert.Equal(LogEventLevel.Error, events[0].Level);
        Assert.NotNull(events[0].Exception);
        Assert.IsType<InvalidOperationException>(events[0].Exception);
    }

    [Fact]
    public void Logger_EnrichmentWithContext()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.WithProperty("Environment", "Test")
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Information("Contextual message");

        Assert.Single(events);
        Assert.Contains("Environment", events[0].Properties.Keys);
    }

    [Fact]
    public void Logger_MinimumLevelFiltering()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        logger.Debug("Debug - should be filtered");
        logger.Information("Info - should be filtered");
        logger.Warning("Warning - should pass");
        logger.Error("Error - should pass");

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.True(e.Level >= LogEventLevel.Warning));
    }

    // Helper sink for testing
    private class CollectingSink : ILogEventSink
    {
        private readonly List<LogEvent> _events;

        public CollectingSink(List<LogEvent> events)
        {
            _events = events;
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}
