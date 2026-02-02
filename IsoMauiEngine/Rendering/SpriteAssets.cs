using Microsoft.Maui.Storage;
using IsoMauiEngine.Diagnostics;

namespace IsoMauiEngine.Rendering;

public static class SpriteAssets
{
	private static readonly SemaphoreSlim Gate = new(1, 1);

	private static bool AreAllReady => EngineerWalking is not null
		&& DeckPlateNormal is not null
		&& Wall0 is not null
		&& RcsConsole0 is not null
		&& RcsConsole1 is not null
		&& RcsConsole2 is not null
		&& LockerEmpty is not null
		&& LockerWithSuit is not null
		&& Door0 is not null
		&& Door1 is not null
		&& Door2 is not null
		&& Door3 is not null;

	public static SpriteSheet? EngineerWalking { get; private set; }
	public static SpriteSheet? SpacesuitDirections { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? DeckPlateNormal { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? Wall0 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? RcsConsole0 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? RcsConsole1 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? RcsConsole2 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? WorldBackground { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? LockerEmpty { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? LockerWithSuit { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? Door0 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? Door1 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? Door2 { get; private set; }
	public static Microsoft.Maui.Graphics.IImage? Door3 { get; private set; }

	public static bool IsReady => EngineerWalking is not null;

	public static void EnsureLoaded()
	{
		_ = EnsureLoadedAsync();
	}

	public static async Task EnsureLoadedAsync()
	{
		if (AreAllReady)
		{
			return;
		}

		await Gate.WaitAsync().ConfigureAwait(false);
		try
		{
			if (AreAllReady)
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
				// Walking sheet: 8 columns (directions) x 6 rows (frames).
				// Column order: N, NE, E, SE, S, SW, W, NW.
				// Row order: animation frames 0..3.
				EngineerWalking = new SpriteSheet(walkingImage, columns: 8, rows: 6);
				RouteDebugLogger.Log($"[Sprite] Loaded EngineerWalking {walkingImage.Width}x{walkingImage.Height}");
			}
			else
			{
				RouteDebugLogger.Log("[Sprite] Failed to load EngineerWalking");
			}

			// Spacesuit directional sheet: 8 columns (directions) x 1 row.
			if (SpacesuitDirections is null)
			{
				var suitImage = await TryLoadImageAsync(
					"sprites/spacesuit_directions.png",
					"spacesuit_directions.png").ConfigureAwait(false);
				if (suitImage is not null)
				{
					SpacesuitDirections = new SpriteSheet(suitImage, columns: 8, rows: 1);
					RouteDebugLogger.Log($"[Sprite] Loaded SpacesuitDirections {suitImage.Width}x{suitImage.Height}");
				}
				else
				{
					RouteDebugLogger.Log("[Sprite] Failed to load SpacesuitDirections");
				}
			}

			// Default ground tile sprite.
			// Note: package filename (Resources/Raw/sprites/deck_plate_normal.png).
			var deckImage = await TryLoadImageAsync(
				"sprites/deck_plate_normal.png",
				"sprites/deck_plate_normal_basic.png").ConfigureAwait(false);

			if (deckImage is not null)
			{
				DeckPlateNormal = deckImage;
				RouteDebugLogger.Log($"[Sprite] Loaded DeckPlateNormal {deckImage.Width}x{deckImage.Height}");
			}
			else
			{
				RouteDebugLogger.Log("[Sprite] Failed to load DeckPlateNormal");
			}

			// Default wall sprite.
			if (Wall0 is null)
			{
				Wall0 = await TryLoadImageAsync(
					"sprites/Wall0.png",
					"sprites/wall0.png").ConfigureAwait(false);
				RouteDebugLogger.Log($"[Sprite] Loaded Wall0={(Wall0 is not null)}");
			}

			// RCS console sprite (tile prop).
			if (RcsConsole0 is null)
			{
				RcsConsole0 = await TryLoadImageAsync(
					"sprites/RCSconsole0.png",
					"sprites/rcsconsole0.png").ConfigureAwait(false);
				RouteDebugLogger.Log($"[Sprite] Loaded RcsConsole0={(RcsConsole0 is not null)}");
			}
			if (RcsConsole1 is null)
			{
				RcsConsole1 = await TryLoadImageAsync(
					"sprites/RCSconsole1.png",
					"sprites/rcsconsole1.png").ConfigureAwait(false);
				RouteDebugLogger.Log($"[Sprite] Loaded RcsConsole1={(RcsConsole1 is not null)}");
			}
			if (RcsConsole2 is null)
			{
				RcsConsole2 = await TryLoadImageAsync(
					"sprites/RCSconsole2.png",
					"sprites/rcsconsole2.png").ConfigureAwait(false);
				RouteDebugLogger.Log($"[Sprite] Loaded RcsConsole2={(RcsConsole2 is not null)}");
			}

			// World background image (screen-space).
			// Note: package filename (Resources/Raw/sprites/MilkyWayPanorama8K.jpg).
			if (WorldBackground is null)
			{
				var bgImage = await TryLoadImageAsync(
					"sprites/MilkyWayPanorama8K.jpg",
					"sprites/milkywaypanorama8k.jpg").ConfigureAwait(false);

				if (bgImage is not null)
				{
					WorldBackground = bgImage;
					RouteDebugLogger.Log($"[Sprite] Loaded WorldBackground {bgImage.Width}x{bgImage.Height}");
				}
				else
				{
					RouteDebugLogger.Log("[Sprite] Failed to load WorldBackground");
				}
			}

			// Locker sprites (tile props).
			if (LockerEmpty is null || LockerWithSuit is null)
			{
				var lockerEmpty = await TryLoadImageAsync(
					"sprites/Locker0.png",
					"sprites/locker0.png").ConfigureAwait(false);
				var lockerSuit = await TryLoadImageAsync(
					"sprites/Locker1.png",
					"sprites/locker1.png").ConfigureAwait(false);

				LockerEmpty = lockerEmpty;
				LockerWithSuit = lockerSuit;
				RouteDebugLogger.Log($"[Sprite] Loaded Locker sprites (Empty={(LockerEmpty is not null)}, WithSuit={(LockerWithSuit is not null)})");
			}

			// Door sprites.
			if (Door0 is null || Door1 is null || Door2 is null || Door3 is null)
			{
				Door0 = await TryLoadImageAsync(
					"sprites/Door0.png",
					"sprites/door0.png").ConfigureAwait(false);
				Door1 = await TryLoadImageAsync(
					"sprites/Door1.png",
					"sprites/door1.png").ConfigureAwait(false);
				Door2 = await TryLoadImageAsync(
					"sprites/Door2.png",
					"sprites/door2.png").ConfigureAwait(false);
				Door3 = await TryLoadImageAsync(
					"sprites/Door3.png",
					"sprites/door3.png").ConfigureAwait(false);
				RouteDebugLogger.Log($"[Sprite] Loaded Door sprites (Door0={(Door0 is not null)}, Door1={(Door1 is not null)}, Door2={(Door2 is not null)}, Door3={(Door3 is not null)})");
			}

			RouteDebugLogger.Log($"[Sprite] Ready={AreAllReady} (Walking={EngineerWalking is not null}, Suit={SpacesuitDirections is not null}, Deck={DeckPlateNormal is not null}, Wall0={Wall0 is not null}, Rcs0={RcsConsole0 is not null}, Rcs1={RcsConsole1 is not null}, Rcs2={RcsConsole2 is not null}, Bg={WorldBackground is not null}, Locker0={LockerEmpty is not null}, Locker1={LockerWithSuit is not null}, Door0={Door0 is not null}, Door1={Door1 is not null}, Door2={Door2 is not null}, Door3={Door3 is not null})");
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
