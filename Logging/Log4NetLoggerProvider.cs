using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;

namespace OverlayIconWatcher.Logging;

public class Log4NetLoggerProvider(string configFileName) : ILoggerProvider
{
	string ConfigFileName { get; } = configFileName;

	ConcurrentDictionary<string, Log4NetLogger> Loggers { get; } = [];

	public ILogger CreateLogger(string categoryName) =>
		Loggers.GetOrAdd(categoryName, CreateLoggerImplementation);

	Log4NetLogger CreateLoggerImplementation(string categoryName)
	{
		var repository = Log4NetHelper.Repository;

		if (log4net.LogManager.GetCurrentLoggers(repository.Name).Length == 0)
			log4net.Config.XmlConfigurator.Configure(repository, new FileInfo(ConfigFileName));

		log4net.ILog log = log4net.LogManager.GetLogger(repository.Name, categoryName);

		var logger = new Log4NetLogger(log);

		return logger;
	}

	public void Dispose()
	{
        Loggers.Clear();
		GC.SuppressFinalize(this);
    }
}
