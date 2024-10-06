using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Interfaces;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Traits.World;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Bridge.Traits;

public enum BridgeAxis { X, Y }
public enum BridgeNodeType { Segment, Head }
public enum BridgeDirection { Positive = 1, Negative = -1 }

public class BridgeNodeInfo : TraitInfo, IRulesetLoaded, Requires<BuildingInfo>
{
	public readonly BridgeAxis Axis;
	public readonly BridgeNodeType Type;
	
	public CVec Offset { get; private set; }

	public override object Create(ActorInitializer init)
	{
		return new BridgeNode(init.Self, this);
	}

	public void RulesetLoaded(Ruleset rules, ActorInfo info)
	{
		var buildingInfo = info.TraitInfo<BuildingInfo>();
		var dimension = buildingInfo.Dimensions;
		Offset = Axis == BridgeAxis.X ? new CVec(dimension.X, 0) : new CVec(0, dimension.Y);
	}
}

public class BridgeNode : IBridgeNode, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyDamageStateChanged
{
	readonly BridgesManager manager;

	public int BridgeId { get; set; } = -1;
	public int Id { get; set; } = -1;
	public BridgeDirection Direction { get; set; }
	public Actor Actor { get; init; }
	public BridgeNodeInfo Info { get; init; }
	public BuildingInfo BuildingInfo { get; init; }
	public IHealth Health { get; init; }
	public IBridgeNode PrevNode { get; set; }
	public IBridgeNode NextNode { get; set; }

	public BridgeNode(Actor self, BridgeNodeInfo info)
	{
		Info = info;
		Actor = self;
		manager = self.World.WorldActor.Trait<BridgesManager>();
		BuildingInfo = self.Info.TraitInfoOrDefault<BuildingInfo>();
		Health = self.TraitsImplementing<IHealth>().FirstOrDefault();
	}

	public void Damage(Damage damage)
	{
		if (Info.Type != BridgeNodeType.Segment)
			return;

		Health?.InflictDamage(Actor, Actor.World.WorldActor, damage, true);
	}

	public void Repair()
	{
		if (Health is null || Health.HP == Health.MaxHP)
			return;

		if (Actor.IsDead)
		{
			(Health as Health)?.Resurrect(Actor, Actor.World.WorldActor);
			return;
		}

		Damage(new Damage(-Health.MaxHP));
	}

	void INotifyAddedToWorld.AddedToWorld(Actor self) => manager.AddNode(this);

	void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) => manager.RemoveNode(this);

	void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
	{
		if (e.Attacker == self.World.WorldActor)
			return;

		manager.OnNodeDamaged(this, self);
	}
}
