using Microsoft.Extensions.Logging;

namespace OverlayIconWatcher.Logging;

public static class Global
{
	static ILoggerFactory? loggerFactory;

	public static ILoggerFactory LoggerFactory =>
		loggerFactory ??= new LoggerFactory().AddLog4Net();
}
