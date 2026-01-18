using System.Numerics;

#if WINDOWS
using Windows.System;
#endif

namespace IsoMauiEngine.Engine;

public sealed class InputState
{
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
		}
	}
#endif
}
