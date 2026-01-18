using System.Collections.Concurrent;

namespace IsoMauiEngine.Navigation;

public sealed class ModuleGraph
{
	public readonly record struct DoorKey(int ModuleId, DoorSide Side);
	public readonly record struct DoorLink(int OtherModuleId, DoorSide OtherSide);

	private readonly ConcurrentDictionary<DoorKey, DoorLink> _links = new();
	private int _version;

	public int Version => _version;

	public bool TryLinkDoors(int moduleA, DoorSide sideA, int moduleB, DoorSide sideB)
	{
		// Only allow opposite-side links (N<->S, E<->W). No rotation support.
		if (!sideA.IsOppositeTo(sideB))
		{
			return false;
		}

		var keyA = new DoorKey(moduleA, sideA);
		var keyB = new DoorKey(moduleB, sideB);

		_links[keyA] = new DoorLink(moduleB, sideB);
		_links[keyB] = new DoorLink(moduleA, sideA);
		Interlocked.Increment(ref _version);
		return true;
	}

	public bool TryLinkDoors(ModuleInstance moduleA, DoorSide sideA, ModuleInstance moduleB, DoorSide sideB)
	{
		return TryLinkDoors(moduleA.ModuleId, sideA, moduleB.ModuleId, sideB);
	}

	public void UnlinkDoor(ModuleInstance module, DoorSide side)
	{
		UnlinkDoor(module.ModuleId, side);
	}

	public List<int> FindModuleRoute(ModuleInstance start, ModuleInstance goal)
	{
		return FindModuleRoute(start.ModuleId, goal.ModuleId);
	}

	public void UnlinkDoor(int module, DoorSide side)
	{
		var key = new DoorKey(module, side);
		if (_links.TryRemove(key, out var link))
		{
			_links.TryRemove(new DoorKey(link.OtherModuleId, link.OtherSide), out _);
			Interlocked.Increment(ref _version);
		}
	}

	public bool TryGetLink(int module, DoorSide side, out DoorLink link)
	{
		return _links.TryGetValue(new DoorKey(module, side), out link);
	}

	public List<(DoorKey A, DoorKey B)> EnumerateUniqueLinksSnapshot()
	{
		var result = new List<(DoorKey A, DoorKey B)>();
		foreach (var kvp in _links)
		{
			var a = kvp.Key;
			var bLink = kvp.Value;
			var b = new DoorKey(bLink.OtherModuleId, bLink.OtherSide);

			// Each link is stored twice; only return one direction.
			if (a.ModuleId < b.ModuleId)
			{
				result.Add((a, b));
				continue;
			}
			if (a.ModuleId == b.ModuleId && (int)a.Side < (int)b.Side)
			{
				result.Add((a, b));
			}
		}
		return result;
	}

	public List<int> FindModuleRoute(int startModuleId, int goalModuleId)
	{
		// BFS over module graph using door links.
		if (startModuleId == goalModuleId)
		{
			return new List<int> { startModuleId };
		}

		var queue = new Queue<int>();
		var prev = new Dictionary<int, int>();
		var visited = new HashSet<int>();
		queue.Enqueue(startModuleId);
		visited.Add(startModuleId);

		while (queue.Count > 0)
		{
			var cur = queue.Dequeue();
			foreach (var next in EnumerateNeighbors(cur))
			{
				if (!visited.Add(next))
				{
					continue;
				}
				prev[next] = cur;
				if (next == goalModuleId)
				{
					return ReconstructRoute(startModuleId, goalModuleId, prev);
				}
				queue.Enqueue(next);
			}
		}

		return new List<int>();
	}

	private IEnumerable<int> EnumerateNeighbors(int moduleId)
	{
		foreach (DoorSide side in Enum.GetValues(typeof(DoorSide)))
		{
			if (_links.TryGetValue(new DoorKey(moduleId, side), out var link))
			{
				yield return link.OtherModuleId;
			}
		}
	}

	private static List<int> ReconstructRoute(int start, int goal, Dictionary<int, int> prev)
	{
		var route = new List<int>();
		var cur = goal;
		route.Add(cur);
		while (cur != start)
		{
			cur = prev[cur];
			route.Add(cur);
		}
		route.Reverse();
		return route;
	}
}
