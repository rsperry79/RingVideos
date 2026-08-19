using System;

using Microsoft.Extensions.Logging;

using Ring.Videos.Logging;

using Serilog.Context;

#nullable enable

namespace Ring.Videos.Logging;

public static class LoggingExtensions
{
    public static IDisposable PushCorrelationId(string correlationId)
    {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    public static IDisposable PushProcessingStep(string stepName)
    {
        return LogContext.PushProperty("ProcessingStep", stepName);
    }

    public static void LogRawResult(this ILogger logger, string eventName, object? result)
    {
        logger.LogInformation("RawResult {eventName} {@result}", eventName, result);
    }

    public static void LogProcessingStep(this ILogger logger, string stepName, object? context = null)
    {
        if (context != null)
        {
            logger.LogInformation("ProcessingStep {stepName} {@context}", stepName, context);
        }
        else
        {
            logger.LogInformation("ProcessingStep {stepName}", stepName);
        }
    }

    public static void LogStructuredEvent(this ILogger logger, string eventType, object eventData)
    {
        logger.LogInformation("StructuredEvent {eventType} {@eventData}", eventType, eventData);
    }

    public static void LogApiCall(this ILogger logger, string method, string url, int statusCode, long durationMs)
    {
        logger.LogInformation("ApiCall {method} {url} Status={statusCode} Duration={durationMs}ms",
            method, url, statusCode, durationMs);
    }

    public static void LogDownloadProgress(this ILogger logger, string videoId, string fileName, long bytesWritten, long? totalBytes)
    {
        logger.LogInformation("DownloadProgress {videoId} {fileName} {bytesWritten}/{totalBytes}",
            videoId, fileName, bytesWritten, totalBytes ?? 0);
    }

    public static void LogError(this ILogger logger, string errorContext, Exception? exception, object? details = null)
    {
        if (details != null)
        {
            logger.LogError(exception, "Error {errorContext} {@details}", errorContext, details);
        }
        else
        {
            logger.LogError(exception, "Error {errorContext}", errorContext);
        }
    }
}
