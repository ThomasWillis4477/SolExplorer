using Microsoft.Maui.Storage;
using IsoMauiEngine.Diagnostics;

namespace IsoMauiEngine.Rendering;

public static class SpriteAssets
{
	private static readonly SemaphoreSlim Gate = new(1, 1);

	public static SpriteSheet? EngineerWalking { get; private set; }

	public static bool IsReady => EngineerWalking is not null;

	public static void EnsureLoaded()
	{
		_ = EnsureLoadedAsync();
	}

	public static async Task EnsureLoadedAsync()
	{
		if (IsReady)
		{
			return;
		}

		await Gate.WaitAsync().ConfigureAwait(false);
		try
		{
			if (IsReady)
			{
				return;
			}

			// Note: these are package filenames (prefer Resources/Raw/sprites/*).
			// We try a couple variants so future renames don't silently break.
			var walkingImage = await TryLoadImageAsync(
				"sprites/engineer_walking.png",
				"engineer_walking.png",
				"Engineer Walking.png",
				"Engineer_Walking.png",
				"Resources/Images/Engineer Walking.png",
				"Resources/Images/Engineer_Walking.png",
				"engineer_walking.png",
				"engineer walking.png").ConfigureAwait(false);

			if (walkingImage is not null)
			{
				// Walking sheet: 8 columns (directions) x 4 rows (frames).
				// Column order: N, NE, E, SE, S, SW, W, NW.
				// Row order: animation frames 0..3.
				EngineerWalking = new SpriteSheet(walkingImage, columns: 8, rows: 6);
				RouteDebugLogger.Log($"[Sprite] Loaded EngineerWalking {walkingImage.Width}x{walkingImage.Height}");
			}
			else
			{
				RouteDebugLogger.Log("[Sprite] Failed to load EngineerWalking");
			}

			RouteDebugLogger.Log($"[Sprite] Ready={IsReady}");
		}
		finally
		{
			try { Gate.Release(); } catch { }
		}
	}

	private static async Task<Microsoft.Maui.Graphics.IImage?> TryLoadImageAsync(params string[] candidateFileNames)
	{
		foreach (var name in candidateFileNames)
		{
			try
			{
				using var stream = await FileSystem.OpenAppPackageFileAsync(name).ConfigureAwait(false);
				var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
				if (image is not null)
				{
					RouteDebugLogger.Log($"[Sprite] Loaded '{name}'");
					return image;
				}
			}
			catch (Exception ex)
			{
				RouteDebugLogger.Log($"[Sprite] Load failed '{name}': {ex.GetType().Name} {ex.Message}");
			}
		}

		return null;
	}
}
