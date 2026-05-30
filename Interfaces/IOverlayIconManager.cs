namespace OverlayIconWatcher.Interfaces;

public interface IOverlayIconManager
{
	Task ReorderKeysAsync(CancellationToken cancellationToken = default);
}
