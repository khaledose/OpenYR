using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Traits;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Interfaces;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Master.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;

public class SlaveMinerInfo : SpawnerSlaveInfo
{
	[Desc("Play this sound when the slave is freed")]
	public readonly string FreeSound;

	public override object Create(ActorInitializer init)
	{
		return new SlaveMiner(this);
	}
}

public class SlaveMiner : SpawnerSlave, INotifyIdle, INotifySlaveMinerTransformed
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

	protected override void Tick(Actor self)
	{
		base.Tick(self);
		if (!self.IsInWorld)
		{
			return;
		}

		if (masterMiner.IsTraitPaused || masterMiner.IsTraitDisabled)
		{
			self.QueueActivity(new WaitFor(() => !masterMiner.IsTraitPaused && !masterMiner.IsTraitDisabled));
		}
	}

	protected override void OnMasterLinkedInner(Actor self, Actor master)
	{
		base.OnMasterLinkedInner(self, master);
		masterMiner = master.TraitOrDefault<MasterMiner>();
		if (!harvester.IsTraitDisabled)
		{
			self.QueueActivity(false, new FindAndDeliverResources(self));
		}
	}

	protected override void OnMasterChangedInner(Actor self)
	{
		base.OnMasterChangedInner(self);

		Game.Sound.Play(SoundType.World, info.FreeSound, self.CenterPosition);
	}

	void INotifyIdle.TickIdle(Actor self)
	{
		if (!self.IsInWorld ||
			master is null ||
			masterMiner.IsTraitPaused ||
			masterMiner.IsTraitDisabled ||
			harvester.IsTraitDisabled)
		{
			return;
		}

		if (masterMiner is MobileMasterMiner)
		{
			self.QueueActivity(false, new EnterMasterMiner(self, Target.FromActor(master), null));
			return;
		}

		if (masterMiner is DeployedMasterMiner)
		{
			self.QueueActivity(new FindAndDeliverResources(self));
			return;
		}
	}

	void INotifySlaveMinerTransformed.OnTransformCompleted(Actor self, MasterMiner masterMiner)
	{
		if (!self.IsInWorld)
		{
			return;
		}

		self.CancelActivity();
	}
}
