using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.RA2.Mechanics.SlaveMiner.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Master.Traits;

public class SlaveMinerInfo : SpawnerSlaveInfo
{
	[Desc("Play this sound when the slave is freed")]
	public readonly string FreeSound;

	public override object Create(ActorInitializer init)
	{
		return new SlaveMiner(this);
	}
}

public class SlaveMiner : SpawnerSlave, ITick, INotifyIdle, INotifyMasterTransformed
{
	readonly SlaveMinerInfo info;
	MasterMiner masterMiner;
	Harvester harvester;

	public SlaveMiner(SlaveMinerInfo info)
		: base(info)
	{
		this.info = info;
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		harvester = self.TraitOrDefault<Harvester>();
	}

	public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
	{
		base.LinkMaster(self, master, spawnerMaster);
		masterMiner = Master.TraitOrDefault<MasterMiner>();
		if (!harvester.IsTraitDisabled)
		{
			self.QueueActivity(false, new FindAndDeliverResources(self));
		}
	}

	public override void OnMasterKilled(Actor self, Actor attacker, SpawnerSlaveDisposal disposal)
	{
		base.OnMasterKilled(self, attacker, disposal);

		if (disposal != SpawnerSlaveDisposal.KillSlaves && !string.IsNullOrEmpty(info.FreeSound) && self.IsInWorld)
		{
			Game.Sound.Play(SoundType.World, info.FreeSound, self.CenterPosition);
		}
	}

	void ITick.Tick(Actor self)
	{
		if (!self.IsInWorld)
		{
			return;
		}

		if (masterMiner.IsTraitPaused || masterMiner.IsTraitDisabled)
		{
			self.QueueActivity(new WaitFor(() => !masterMiner.IsTraitPaused && !masterMiner.IsTraitDisabled));
		}
	}

	void INotifyIdle.TickIdle(Actor self)
	{
		if (!self.IsInWorld ||
			Master is null ||
			masterMiner.IsTraitPaused ||
			masterMiner.IsTraitDisabled ||
			harvester.IsTraitDisabled)
		{
			return;
		}

		if (masterMiner is MobileMasterMiner)
		{
			self.QueueActivity(false, new EnterMasterMiner(self, Target.FromActor(Master), null));
		}
		else if (masterMiner is DeployedMasterMiner)
		{
			self.QueueActivity(new FindAndDeliverResources(self));
		}
	}

	void INotifyMasterTransformed.OnMasterTransformed(Actor self)
	{
		if (!self.IsInWorld)
		{
			return;
		}

		self.CancelActivity();
	}
}
