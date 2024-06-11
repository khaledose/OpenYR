using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;

public class SpawnerSlaveInfo : PausableConditionalTraitInfo
{
	public override object Create(ActorInitializer init) { return new SpawnerSlave(this); }
}

public class SpawnerSlave : PausableConditionalTrait<SpawnerSlaveInfo>, ILinkSpawners, INotifyMasterChanged, INotifyKilled, INotifyOwnerChanged
{
	protected SpawnerMaster MasterSpawner;

	public SpawnerSlave(SpawnerSlaveInfo info) : base(info)
	{
	}

	void ILinkSpawners.Link(Actor self, Actor master, MasterSpawner spawner)
	{
		MasterSpawner = spawner;
	}


	void INotifyMasterChanged.OnMasterKilled(Actor self)
	{
	}

	void INotifyMasterChanged.OnMasterOwnerChanged(Actor self)
	{
	}

	void INotifyKilled.Killed(Actor self, AttackInfo e)
	{

	}

	void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
	{

	}
}
