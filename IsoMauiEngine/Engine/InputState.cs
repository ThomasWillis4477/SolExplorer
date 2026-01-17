using System.Numerics;

#if WINDOWS
using Windows.System;
#endif

namespace IsoMauiEngine.Engine;

public sealed class InputState
{
	private bool _left;
	private bool _right;
	private bool _up;
	private bool _down;
	private bool _debugToggleDown;

	public bool DebugOverlayEnabled { get; private set; }

#if WINDOWS
	public void SetKey(VirtualKey key, bool isDown)
	{
		switch (key)
		{
			case VirtualKey.F1:
				if (isDown && !_debugToggleDown)
				{
					DebugOverlayEnabled = !DebugOverlayEnabled;
				}
				_debugToggleDown = isDown;
				break;
			case VirtualKey.Left:
			case VirtualKey.A:
				_left = isDown;
				break;
			case VirtualKey.Right:
			case VirtualKey.D:
				_right = isDown;
				break;
			case VirtualKey.Up:
			case VirtualKey.W:
				_up = isDown;
				break;
			case VirtualKey.Down:
			case VirtualKey.S:
				_down = isDown;
				break;
		}
	}
#endif

	public Vector2 GetMoveVector()
	{
		var x = 0f;
		var y = 0f;

		if (_left) x -= 1f;
		if (_right) x += 1f;
		if (_up) y -= 1f;
		if (_down) y += 1f;

		var v = new Vector2(x, y);
		if (v.LengthSquared() > 1f)
		{
			v = Vector2.Normalize(v);
		}
		return v;
	}
}
