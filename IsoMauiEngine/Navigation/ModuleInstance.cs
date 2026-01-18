using System.Numerics;
using IsoMauiEngine.World.Modules;

namespace IsoMauiEngine.Navigation;

public enum DamageState
{
	Intact,
	Damaged,
	Destroyed
}

/// <summary>
/// Logic-only module representation used by navigation.
/// For now this is backed by <see cref="ShipModuleInstance"/> to avoid duplicating state.
/// </summary>
public sealed class ModuleInstance
{
	public ModuleInstance(ShipModuleInstance backing)
	{
		Backing = backing;
		ModuleId = backing.ModuleId;
		SizePreset = backing.SizePreset;
		GridWidth = backing.Width;
		GridHeight = backing.Height;
		DamageState = DamageState.Intact;
	}

	public int ModuleId { get; }
	public ModuleSizePreset SizePreset { get; }
	public int GridWidth { get; }
	public int GridHeight { get; }
	public DamageState DamageState { get; set; }

	public ShipModuleInstance Backing { get; }

	public Vector2 WorldPosition => Backing.GetWorldCenter();
}
