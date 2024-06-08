using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;

public class SpawnerSlaveInfo : PausableConditionalTraitInfo
{
	public override object Create(ActorInitializer init) { return new SpawnerSlave(this); }
}

public class SpawnerSlave : PausableConditionalTrait<SpawnerSlaveInfo>, INotifyMasterChanged
{
	public SpawnerSlave(SpawnerSlaveInfo info) : base(info)
	{
	}

	public void OnMasterKilled(Actor self)
	{
	}

	public void OnMasterOwnerChanged(Actor self)
	{
	}
}
