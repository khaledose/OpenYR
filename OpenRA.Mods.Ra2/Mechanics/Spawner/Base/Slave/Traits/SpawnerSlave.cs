using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;

public class SpawnerSlaveInfo : PausableConditionalTraitInfo
{
	public override object Create(ActorInitializer init) { return new SpawnerSlave(this); }
}

public class SpawnerSlave : PausableConditionalTrait<SpawnerSlaveInfo>, INotifySlaveLinked, INotifyMasterChanged, INotifyKilled, INotifyOwnerChanged
{
	protected Actor master;
	protected INotifySlaveChanged slaveChanged;

	public SpawnerSlave(SpawnerSlaveInfo info) : base(info)
	{
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
	}

	void INotifySlaveLinked.Link(Actor self, Actor master)
	{
		this.master = master;
		slaveChanged = master.TraitsImplementing<INotifySlaveChanged>().FirstOrDefault();
	}


	void INotifyMasterChanged.OnMasterKilled(Actor self)
	{
	}

	void INotifyMasterChanged.OnMasterOwnerChanged(Actor self)
	{
	}

	void INotifyKilled.Killed(Actor self, AttackInfo e)
	{
		slaveChanged.OnSlaveKilled(self);
	}

	void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
	{
		slaveChanged.OnSlaveOwnerChanged(self);
	}
}
