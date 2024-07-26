using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;

public class MobileMasterMinerInfo : MasterMinerInfo, Requires<MobileInfo>
{
	[Desc("Color to use for the target line of deploy near resources orders.")]
	public readonly Color DeployLineColor = Color.Crimson;

	[Desc("Multiplier used for close locations when searching for resources.")]
	public readonly float CloseDistanceMultiplier = 1.8f;

	[Desc("Multiplier used for far locations when searching for resources.")]
	public readonly float FarDistanceMultiplier = 0.8f;

	[Desc("Max allowed cost for the cost function used by the pathfinder.")]
	public readonly int MaxResourceCost = 100;

	[Desc("Max allowed failed attempts to search for a suitable deployment location.")]
	public readonly int MaxFailedSearches = 3;

	public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
	{
		base.RulesetLoaded(rules, ai);
		var masterActorInfo = GetTransformsActorInfo(rules, ai);
		var deployedMinerInfo = masterActorInfo.TraitInfoOrDefault<DeployedMasterMinerInfo>();
		if (deployedMinerInfo is null)
			throw new YamlException($"Actor {masterActorInfo.Name} must implement {typeof(DeployedMasterMinerInfo)}!");
	}

	public override object Create(ActorInitializer init)
	{
		return new MobileMasterMiner(init, this);
	}
}

public class MobileMasterMiner : MasterMiner, INotifyIdle
{
	public new readonly MobileMasterMinerInfo Info;
	public bool WaitingForPickup => LinkedSlaves.Any(s => s.IsAlive);
	readonly ActorInfo actorInfo;
	readonly BuildingInfo buildingInfo;
	[Sync]
	int scanDelay = 0;

	public MobileMasterMiner(ActorInitializer init, MobileMasterMinerInfo info)
		: base(init, info)
	{
		Info = info;
		actorInfo = Self.World.Map.Rules.Actors[Transforms.Info.IntoActor];
		buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		if (OrderLocation != null)
		{
			DeployNearResources(self, false);
			return;
		}
	}

	protected override void ResolveHarvestOrder(Actor self, Order order)
	{
		base.ResolveHarvestOrder(self, order);
		DeployNearResources(self, order.Queued);
	}

	public void DeployNearResources(Actor self, bool queued)
	{
		self.QueueActivity(queued, new DeployNearResources(self, OrderLocation));
		self.ShowTargetLines();
		orderLocation = null;
	}

	public bool CanDeploy(Actor self, CPos loc)
	{
		if (IsTraitPaused || IsTraitDisabled)
			return false;

		if (Transforms.IsTraitPaused || Transforms.IsTraitDisabled)
			return false;

		return buildingInfo == null || self.World.CanPlaceBuilding(loc, actorInfo, buildingInfo, self);
	}

	void INotifyIdle.TickIdle(Actor self)
	{
		if (IsTraitPaused || IsTraitDisabled)
			return;

		if (scanDelay > 0)
		{
			scanDelay--;
			return;
		}

		scanDelay = Info.ScanDelay;
		DeployNearResources(self, false);
	}
}
