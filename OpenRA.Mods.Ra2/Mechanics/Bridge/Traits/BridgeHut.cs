using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Interfaces;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Traits.World;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Bridge.Traits;

[Desc("Allows bridges to be targeted for demolition and repair.")]
public class BridgeHutInfo : TraitInfo, IDemolishableInfo
{
	public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return false; }

	public override object Create(ActorInitializer init) { return new BridgeHut(init, this); }
}

public class BridgeHut : IBridgeHut
{
	readonly BridgesManager manager;
	public int BridgeId { get; set; } = -1;
	public BridgeDirection Direction { get; set; }
	public Actor Actor { get; init; }
	public BridgeHutInfo Info { get; init; }

	public BridgeHut(ActorInitializer init, BridgeHutInfo info)
	{
		Info = info;
		Actor = init.Self;
		manager = init.World.WorldActor.Trait<BridgesManager>();
	}

	public void Demolish(Actor self)
	{
		throw new NotImplementedException();
	}

	public void Repair(Actor self)
	{
		throw new NotImplementedException();
	}
}
