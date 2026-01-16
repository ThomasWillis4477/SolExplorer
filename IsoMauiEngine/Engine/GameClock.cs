namespace IsoMauiEngine.Engine;

public sealed class GameClock
{
	private readonly float _fixedStepSeconds;
	private float _accumulatorSeconds;

	public GameClock(float fixedStepSeconds = 1f / 60f)
	{
		_fixedStepSeconds = fixedStepSeconds;
	}

	public float DeltaTime => _fixedStepSeconds;

	public int Step(float frameSeconds)
	{
		if (frameSeconds < 0)
		{
			frameSeconds = 0;
		}

		// Prevent spiral of death after long pauses.
		if (frameSeconds > 0.25f)
		{
			frameSeconds = 0.25f;
		}

		_accumulatorSeconds += frameSeconds;
		var steps = 0;

		while (_accumulatorSeconds >= _fixedStepSeconds)
		{
			_accumulatorSeconds -= _fixedStepSeconds;
			steps++;
			if (steps >= 8)
			{
				// Hard cap per frame.
				_accumulatorSeconds = 0;
				break;
			}
		}

		return steps;
	}
}
