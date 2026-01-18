namespace IsoMauiEngine.Navigation;

public static class AStarGrid
{
	public readonly record struct Cell(int X, int Y);

	public static List<Cell> FindPath(
		Cell start,
		Cell goal,
		Func<int, int, bool> isWalkable,
		Func<int, int, bool> isInBounds)
	{
		if (start.Equals(goal))
		{
			return new List<Cell> { start };
		}
		if (!isInBounds(start.X, start.Y) || !isInBounds(goal.X, goal.Y))
		{
			return new List<Cell>();
		}
		if (!isWalkable(goal.X, goal.Y))
		{
			return new List<Cell>();
		}

		var open = new PriorityQueue<Cell, int>();
		var cameFrom = new Dictionary<Cell, Cell>();
		var gScore = new Dictionary<Cell, int>
		{
			[start] = 0
		};

		open.Enqueue(start, Heuristic(start, goal));
		var inOpen = new HashSet<Cell> { start };

		while (open.Count > 0)
		{
			var current = open.Dequeue();
			inOpen.Remove(current);

			if (current.Equals(goal))
			{
				return Reconstruct(cameFrom, current);
			}

			var currentG = gScore[current];
			foreach (var n in Enumerate4(current))
			{
				if (!isInBounds(n.X, n.Y))
				{
					continue;
				}
				if (!isWalkable(n.X, n.Y) && !n.Equals(goal))
				{
					continue;
				}

				var tentative = currentG + 1;
				if (!gScore.TryGetValue(n, out var prevG) || tentative < prevG)
				{
					cameFrom[n] = current;
					gScore[n] = tentative;
					var f = tentative + Heuristic(n, goal);
					if (inOpen.Add(n))
					{
						open.Enqueue(n, f);
					}
					else
					{
						// PriorityQueue has no decrease-key; enqueue duplicate.
						open.Enqueue(n, f);
					}
				}
			}
		}

		return new List<Cell>();
	}

	private static IEnumerable<Cell> Enumerate4(Cell c)
	{
		yield return new Cell(c.X + 1, c.Y);
		yield return new Cell(c.X - 1, c.Y);
		yield return new Cell(c.X, c.Y + 1);
		yield return new Cell(c.X, c.Y - 1);
	}

	private static int Heuristic(Cell a, Cell b)
	{
		return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
	}

	private static List<Cell> Reconstruct(Dictionary<Cell, Cell> cameFrom, Cell current)
	{
		var path = new List<Cell> { current };
		while (cameFrom.TryGetValue(current, out var prev))
		{
			current = prev;
			path.Add(current);
		}
		path.Reverse();
		return path;
	}
}
