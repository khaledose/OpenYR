using System.Linq;
using OpenRA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Master.Traits;

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

public class DeployedMasterMiner : MasterMiner, ITick
{
	public new readonly DeployedMasterMinerInfo Info;
	bool initialSpawn = true;

	[Sync]
	int respawnTicks = -1;
	[Sync]
	int scanTicks = -1;

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

	void ITick.Tick(Actor self)
	{
		if (IsTraitPaused || IsTraitDisabled)
		{
			return;
		}

		ReplenishDeadSlaves(self);
		if (ScanResourcesTick(self))
		{
			return;
		}

		RespawnSlavesTick(self);
	}

	bool ScanResourcesTick(Actor self)
	{
		if (scanTicks > 0)
		{
			scanTicks--;
			return false;
		}

		scanTicks = Info.ScanDelay;

		var density = GetResourceDensityAtLocation(self.Location);
		if (density < 10 * Info.ScanRadius && Transforms.CanDeploy())
		{
			self.QueueActivity(false, Transforms.GetTransformActivity());
			return true;
		}

		return false;
	}

	void RespawnSlavesTick(Actor self)
	{
		var disposedSlaves = SlaveEntries.Where(s => s.IsValid && !s.Actor.IsInWorld).ToArray();

		if (initialSpawn)
		{
			foreach (var slave in disposedSlaves)
			{
				SpawnIntoWorld(self, slave.Actor, self.CenterPosition);
			}

			initialSpawn = false;
			return;
		}

		if (disposedSlaves.Length > 0 && respawnTicks == 0)
		{
			var slaveEntry = disposedSlaves.FirstOrDefault();
			SpawnIntoWorld(self, slaveEntry.Actor, self.CenterPosition);
		}

		if (disposedSlaves.Length > 0 && respawnTicks < 0)
		{
			respawnTicks = Info.RespawnTicks;
		}

		respawnTicks--;
	}
}
