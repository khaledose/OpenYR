using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Interfaces;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Traits;

public class SpawnerSlaveInfo : PausableConditionalTraitInfo
{
	[GrantedConditionReference]
	[Desc("The condition to grant when master miner is killed.")]
	public readonly string MasterKilledCondition = null;

	public override object Create(ActorInitializer init) { return new SpawnerSlave(this); }
}

public class SpawnerSlave : PausableConditionalTrait<SpawnerSlaveInfo>, ITick, ILinkSlaveWithMaster, INotifyMasterChanged, INotifyActorDisposing, INotifyOwnerChanged
{
	protected Actor master;
	protected SpawnerMaster spawnerMaster;
	protected INotifySlaveChanged slaveChanged;
	protected int masterKilledToken = Actor.InvalidConditionToken;

	public SpawnerSlave(SpawnerSlaveInfo info) : base(info){}

	protected override void Created(Actor self)
	{
		base.Created(self);
	}

	protected virtual void Tick(Actor self){}

	protected virtual void OnMasterKilledInner(Actor self)
	{
		if (string.IsNullOrEmpty(Info.MasterKilledCondition))
			return;

		if (masterKilledToken == Actor.InvalidConditionToken)
			masterKilledToken = self.GrantCondition(Info.MasterKilledCondition);
	}

	protected virtual void OnMasterOwnerChangedInner(Actor self) { }

	protected virtual void LinkSlaveWithMasterInner(Actor self, Actor master)
	{
		this.master = master;
		slaveChanged = master.TraitsImplementing<INotifySlaveChanged>().FirstOrDefault();
		spawnerMaster = master.TraitOrDefault<SpawnerMaster>();
	}

	void ITick.Tick(Actor self)
	{
		Tick(self);
	}

	void ILinkSlaveWithMaster.Link(Actor self, Actor master)
	{
		LinkSlaveWithMasterInner(self, master);
	}

	void INotifyMasterChanged.OnMasterKilled(Actor self)
	{
		OnMasterKilledInner(self);
	}

	void INotifyMasterChanged.OnMasterOwnerChanged(Actor self)
	{
		OnMasterOwnerChangedInner(self);
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
