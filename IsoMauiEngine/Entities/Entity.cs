using System.Numerics;
using IsoMauiEngine.Engine;
using IsoMauiEngine.Rendering;

namespace IsoMauiEngine.Entities;

public abstract class Entity
{
	public Vector2 WorldPos { get; set; }

	public virtual void Update(float dt, InputState input)
	{
	}

	public virtual void EmitDrawItems(List<DrawItem> drawItems)
	{
	}
}
