using System.Reflection;
using Microsoft.Extensions.Logging;

namespace OverlayIconWatcher.Logging;

public static class Log4NetLoggerFactoryExtensions
{
	public static ILoggingBuilder AddLog4Net(this ILoggingBuilder builder)
	{
		builder.SetMinimumLevel(LogLevel.Trace);
		builder.AddProvider(CreateLog4NetProvider(null));

		return builder;
	}

	public static ILoggerFactory AddLog4Net(this ILoggerFactory factory)
	{
		factory.AddProvider(CreateLog4NetProvider(null));

		return factory;
	}

	static ILoggerProvider CreateLog4NetProvider(string? configFileName)
	{
		string fileNameOrPath;

		if (string.IsNullOrEmpty(configFileName))
		{
			fileNameOrPath = Log4NetHelper.DefaultConfigurationFileName;

			if (!File.Exists(fileNameOrPath))
			{
				string? assemblyLocation = Assembly.GetEntryAssembly()?.Location ?? throw new Exception("Location of entry assembly is null)");
				var path = new FileInfo(assemblyLocation).Directory?.FullName ?? throw new Exception("Directory of entry assembly is null");

				fileNameOrPath = Path.Combine(path, fileNameOrPath);
			}
		}
		else
			fileNameOrPath = configFileName;

		return new Log4NetLoggerProvider(fileNameOrPath);
	}
}
