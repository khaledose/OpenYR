using OpenRA.Mods.Common.Activities;
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

	public SlaveMiner(SlaveMinerInfo info)
		: base(info)
	{
		this.info = info;
	}

	protected override void OnMasterKilledInner(Actor self)
	{
		base.OnMasterKilledInner(self);
		Game.Sound.Play(SoundType.World, info.FreeSound, self.CenterPosition);
	}

	void Work(Actor self)
	{
		if (spawnerMaster is MobileMasterMiner)
		{
			self.QueueActivity(false, new EnterMasterMiner(self, Target.FromActor(master), null));
			return;
		}

		if (spawnerMaster is DeployedMasterMiner)
		{
			self.QueueActivity(new FindAndDeliverResources(self));
			return;
		}
	}

	void INotifyIdle.TickIdle(Actor self)
	{
		Work(self);
	}

	void INotifySlaveMinerTransformed.OnTransformCompleted(Actor self, MasterMiner masterMiner)
	{
		spawnerMaster = masterMiner;
		Work(self);
	}
}
