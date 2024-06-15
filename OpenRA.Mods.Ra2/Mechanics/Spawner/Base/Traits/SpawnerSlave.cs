using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Interfaces;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Traits;

public class SpawnerSlaveInfo : PausableConditionalTraitInfo
{
	public override object Create(ActorInitializer init) { return new SpawnerSlave(this); }
}

public class SpawnerSlave : PausableConditionalTrait<SpawnerSlaveInfo>, ITick, INotifySlaveLinked, INotifyMasterChanged, INotifyActorDisposing, INotifyOwnerChanged
{
	protected Actor master;
	protected INotifySlaveChanged slaveChanged;

	public SpawnerSlave(SpawnerSlaveInfo info) : base(info){}

	protected override void Created(Actor self)
	{
		base.Created(self);
	}

	protected virtual void Tick(Actor self){}

	protected virtual void OnMasterChangedInner(Actor self){}

	protected virtual void OnMasterLinkedInner(Actor self, Actor master)
	{
		this.master = master;
		slaveChanged = master.TraitsImplementing<INotifySlaveChanged>().FirstOrDefault();
	}

	void ITick.Tick(Actor self)
	{
		Tick(self);
	}

	void INotifySlaveLinked.Link(Actor self, Actor master)
	{
		OnMasterLinkedInner(self, master);
	}

	void INotifyMasterChanged.OnMasterKilled(Actor self)
	{
		OnMasterChangedInner(self);
	}

	void INotifyMasterChanged.OnMasterOwnerChanged(Actor self)
	{
		OnMasterChangedInner(self);
	}

	void INotifyActorDisposing.Disposing(Actor self)
	{
		slaveChanged.OnSlaveKilled(self);
	}

	void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
	{
		slaveChanged.OnSlaveOwnerChanged(self);
	}
}
