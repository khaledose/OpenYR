using OpenRA.Mods.Common.Traits;

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

	public DeployedMasterMiner(ActorInitializer init, DeployedMasterMinerInfo info)
		: base(init, info)
	{
		Info = info;
	}

	protected override void ResolveOrderInner(Actor self, Order order)
	{
		base.ResolveOrderInner(self, order);

		self.QueueActivity(false, Transforms.GetTransformActivity());
	}

	protected override void AfterTransformInner(Actor newMaster)
	{
		base.AfterTransformInner(newMaster);

		if (orderLocation.HasValue)
		{
			var masterMiner = newMaster.TraitOrDefault<MobileMasterMiner>();
			masterMiner.DeployNearResources(newMaster, true);
		}
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
		if (scanTicks > 0)
		{
			scanTicks--;
			return false;
		}

		scanTicks = Info.ScanDelay;

		var density = GetResourcesDensity(self.Location);
		if (density < 10 * Info.ScanRadius && Transforms.CanDeploy())
		{
			self.QueueActivity(false, Transforms.GetTransformActivity());
			return true;
		}

		return false;
	}
}
