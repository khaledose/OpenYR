using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Extensions;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;

public class DeployedMasterMinerInfo : MasterMinerInfo
{
	public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
	{
		base.RulesetLoaded(rules, ai);
		var masterActorInfo = GetTransformsActorInfo(rules, ai);
		var mobileMinerInfo = masterActorInfo.TraitInfoOrDefault<MobileMasterMinerInfo>();
		if (mobileMinerInfo is null)
			throw new YamlException($"Actor {masterActorInfo.Name} must implement {typeof(MobileMasterMinerInfo)}!");
	}

	public override object Create(ActorInitializer init)
	{
		return new DeployedMasterMiner(init, this);
	}
}

public class DeployedMasterMiner : MasterMiner
{
	public new readonly DeployedMasterMinerInfo Info;

	[Sync]
	int scanTicks;
	float initialNearbyResources;

	public DeployedMasterMiner(ActorInitializer init, DeployedMasterMinerInfo info)
		: base(init, info)
	{
		Info = info;
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		initialNearbyResources = ResourceLayer.GetDensityInRadius(self.World, self.Location, Info.ScanRadius, CanHarvestCell);
	}

	protected override void ResolveHarvestOrder(Actor self, Order order)
	{
		base.ResolveHarvestOrder(self, order);

		self.QueueActivity(false, Transforms.GetTransformActivity());
	}

	protected override void Tick(Actor self)
	{
		base.Tick(self);

		if (IsTraitPaused || IsTraitDisabled || ScanResourcesTick(self))
		{
			return;
		}

		var readySlaves = LinkedSlaves.Where(s => s.IsReady);
		foreach (var slave in readySlaves)
		{
			SpawnSlave(self, slave.Actor);
		}
	}

	bool ScanResourcesTick(Actor self)
	{
		const float MinThreshold = 0.05f;  // 5%
		const float MaxThreshold = 0.5f;   // 50%
		const float ScalingFactor = 0.1f;  // Scaling factor for dynamic threshold

		if (scanTicks > 0)
		{
			scanTicks--;
			return false;
		}

		scanTicks = Info.ScanDelay;

		// Get the current resource density at the current location
		var currentNearbyResources = ResourceLayer.GetDensityInRadius(self.World, self.Location, Info.ScanRadius, CanHarvestCell);

		// Calculate the dynamic threshold using an exponential decay function
		var dynamicThreshold = (float)Math.Exp(-ScalingFactor * initialNearbyResources);

		// Clamp the threshold between MinThreshold and MaxThreshold
		dynamicThreshold = Math.Max(MinThreshold, Math.Min(MaxThreshold, dynamicThreshold));

		// Check if the current density has fallen below the dynamic threshold
		if (initialNearbyResources == 0 || currentNearbyResources / initialNearbyResources <= dynamicThreshold)
		{
			self.QueueActivity(false, Transforms.GetTransformActivity());
			initialNearbyResources = 0;  // Reset for the next deployment
			return true;
		}

		return false;
	}

}
