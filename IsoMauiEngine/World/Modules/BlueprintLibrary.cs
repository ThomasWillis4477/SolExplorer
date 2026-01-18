namespace IsoMauiEngine.World.Modules;

public static class BlueprintLibrary
{
	public static ModuleBlueprint Get(ModuleSizePreset preset)
	{
		return preset switch
		{
			ModuleSizePreset.Small => new ModuleBlueprint(width: 6, height: 6),
			ModuleSizePreset.Medium => new ModuleBlueprint(width: 11, height: 11),
			ModuleSizePreset.Large => new ModuleBlueprint(width: 16, height: 16),
			_ => new ModuleBlueprint(width: 11, height: 11)
		};
	}
}
