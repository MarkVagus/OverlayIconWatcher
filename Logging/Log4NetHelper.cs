using System.Reflection;

namespace OverlayIconWatcher.Logging;

public static class Log4NetHelper
{
	static log4net.Repository.ILoggerRepository? repository = null;

	public static log4net.Repository.ILoggerRepository Repository =>
		repository ??= log4net.LogManager.GetRepository(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

	static public string DefaultConfigurationFileName { get; set; } = "log4net.config";
}
