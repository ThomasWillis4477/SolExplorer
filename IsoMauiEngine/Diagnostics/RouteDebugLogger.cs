using System.Diagnostics;
using System.Text;
using Microsoft.Maui.Storage;

namespace IsoMauiEngine.Diagnostics;

public static class RouteDebugLogger
{
	private static readonly SemaphoreSlim Gate = new(1, 1);

	public static string LogFilePath => Path.Combine(FileSystem.AppDataDirectory, "route-navigation.log.txt");

	public static void Log(string message)
	{
		_ = LogAsync(message);
	}

	public static async Task LogAsync(string message)
	{
		var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";

		try
		{
			await Gate.WaitAsync().ConfigureAwait(false);

			var dir = Path.GetDirectoryName(LogFilePath);
			if (!string.IsNullOrWhiteSpace(dir))
			{
				Directory.CreateDirectory(dir);
			}

			await File.AppendAllTextAsync(LogFilePath, line, Encoding.UTF8).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[RouteDebugLogger] Failed to write log: {ex}");
		}
		finally
		{
			try { Gate.Release(); } catch { }
		}
	}
}
