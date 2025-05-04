using Microsoft.Extensions.Logging;

namespace OverlayIconWatcher.Logging;

public class Log4NetLogger(log4net.ILog log) : ILogger
{
	readonly log4net.ILog log = log;

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace or LogLevel.Debug => log.IsDebugEnabled,
			LogLevel.Information => log.IsInfoEnabled,
			LogLevel.Warning => log.IsWarnEnabled,
			LogLevel.Error => log.IsErrorEnabled,
			LogLevel.Critical => log.IsFatalEnabled,
			_ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
		};
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
			return;

		if (formatter is null)
			throw new ArgumentException(null, nameof(formatter));

		string message = formatter(state, exception);

		if (!string.IsNullOrEmpty(message))
			WriteMessage(logLevel, eventId.Id, message, exception);
	}

	void WriteMessage(LogLevel logLevel, int eventId, string message, Exception? exception)
	{
		string evtId = eventId == 0 ? string.Empty : $" [{eventId}]";

		switch (logLevel)
		{
			case LogLevel.Trace:
			case LogLevel.Debug:
				log.Debug($"{message}{evtId}", exception);
				break;
			case LogLevel.Information:
				log.Info($"{message}{evtId}", exception);
				break;
			case LogLevel.Warning:
				log.Warn($"{message}{evtId}", exception);
				break;
			case LogLevel.Error:
				log.Error($"{message}{evtId}", exception);
				break;
			case LogLevel.Critical:
				log.Fatal($"{message}{evtId}", exception);
				break;
			default:
				log.Debug($"{message}{evtId}", exception);
				break;
		}

	}
}
