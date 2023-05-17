using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Runtime.ExceptionServices;
using System.Text;

namespace NuSocial.Services;

/// <summary>
/// The default <see langword="interface"/> for an analytics service.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Logs an event with a specified title and optional properties.
    /// </summary>
    /// <param name="title">The title of the event to track.</param>
    /// <param name="data">The optional event properties.</param>
    void Log(string title, params (string property, object? value)[]? data);

    /// <summary>
    /// Logs an exception with optional properties.
    /// </summary>
    /// <param name="exception">The exception that has been thrown.</param>
    /// <param name="data">The optional event properties.</param>
    void Log(Exception exception, params (string property, object? value)[]? data);
}

/// <summary>
/// An <see cref="IAnalyticsService"/> implementation using AppCenter.
/// </summary>
public sealed class AppCenterService : IAnalyticsService
{
    /// <summary>
    /// The maximum length for any property name and value
    /// </summary>
    /// <remarks>It's 125, but one character is reserved for the leading '|' to indicate trimming.</remarks>
    private const int _propertyStringMaxLength = 124;

    /// <inheritdoc/>
    public AppCenterService(string secret)
    {
        AppCenter.Start(secret, typeof(Crashes), typeof(Analytics));
    }

    /// <inheritdoc/>
    public void Log(string title, params (string property, object? value)[]? data)
    {
        Analytics.TrackEvent(title, GetProperties(data));
    }

    /// <inheritdoc/>
    public void Log(Exception exception, params (string property, object? value)[]? data)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        Crashes.TrackError(exception.Demystify(), GetProperties(data));
    }

    internal static void Exception(UnhandledExceptionEventArgs e)
    {
        var ac = Ioc.Default.GetService<IAnalyticsService>();
        if (ac != null && e.ExceptionObject is Exception ex)
        {
            ac.Log(exception: ex, new (string property, object? value)[]
            {
                ("IsTerminating", e.IsTerminating),
            });
        }
    }

    internal static void Exception(UnobservedTaskExceptionEventArgs error)
    {
        var ac = Ioc.Default.GetService<IAnalyticsService>();
        if (ac != null && error.Exception is AggregateException e)
        {
            Exception ex = e;
            while (ex is AggregateException)
            {
                if (e.InnerException != null)
                    ex = e.InnerException;
            }
            ac.Log(exception: ex, new (string property, object? value)[]
            {
                ("Observed", error.Observed),
            });
        }
    }

    internal static void Exception(FirstChanceExceptionEventArgs error)
    {
        var ac = Ioc.Default.GetService<IAnalyticsService>();
        if (ac != null && error.Exception is Exception ex)
        {
            ac.Log(exception: ex);
        }
    }

    /// <summary>
    /// Gets the additional logging properties from the input data.
    /// </summary>
    /// <param name="data">The optional event properties.</param>
    /// <returns>The additional logging properties to track.</returns>
    private static IDictionary<string, string>? GetProperties((string property, object? value)[]? data)
    {
        return
            data?.ToDictionary(
            pair => pair.property,
            pair =>
            {
                var text = (pair.value ?? "<NULL>").ToString();

                return text?.Length <= _propertyStringMaxLength
                    ? text
                    : $"|{text?[^_propertyStringMaxLength..]}";
            });
    }
}

/// <summary>
/// A <see langword="class"/> that manages the analytics service in a test environment.
/// </summary>
public sealed class DebugAnalyticsService : IAnalyticsService
{
    /// <inheritdoc/>
    public void Log(string title, params (string property, object? value)[]? data)
    {
        StringBuilder builder = new();

        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"[EVENT]: \"{title}\"");

        if (data is not null)
        {
            foreach ((string property, object? value) in data)
            {
                _ = builder.AppendLine(CultureInfo.InvariantCulture, $">> {property}: \"{value ?? "<NULL>"}\"");
            }
        }

        Trace.Write(builder);
    }

    /// <inheritdoc/>
    public void Log(Exception exception, params (string property, object? value)[]? data)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        StringBuilder builder = new();

        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"[EXCEPTION]: \"{exception.Demystify().GetType()}\"");
        _ = builder.AppendLine(">> Stack trace");
        _ = builder.AppendLine(exception.Demystify().StackTrace);

        if (data is not null)
        {
            foreach ((string property, object? value) in data)
            {
                _ = builder.AppendLine(CultureInfo.InvariantCulture, $">> {property}: \"{value ?? "<NULL>"}\"");
            }
        }

        Trace.Write(builder);
    }
}