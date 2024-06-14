using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Mechanics.SlaveMiner.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Master.Traits;

public class MobileMasterMinerInfo : MasterMinerInfo
{
	[Desc("Color to use for the target line of deploy near resources orders.")]
	public readonly Color DeployLineColor = Color.Crimson;

	public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
	{
		base.RulesetLoaded(rules, ai);
		var masterActorInfo = GetTransformsActorInfo(rules, ai);
		var deployedMinerInfo = masterActorInfo.TraitInfoOrDefault<DeployedMasterMinerInfo>();
		if (deployedMinerInfo is null)
			throw new YamlException($"Actor {masterActorInfo.Name} must implement {typeof(DeployedMasterMinerInfo)}!");
	}

	public override object Create(ActorInitializer init)
	{
		return new MobileMasterMiner(init, this);
	}
}

public class MobileMasterMiner : MasterMiner, ITick, INotifySlaveEntering, INotifyIdle
{
	public new readonly MobileMasterMinerInfo Info;
	public bool WaitingForPickup => SlaveEntries.Any(s => s.Actor.IsInWorld);

	public MobileMasterMiner(ActorInitializer init, MobileMasterMinerInfo info)
		: base(init, info)
	{
		Info = info;
	}

	protected override void ResolveOrderInner(Actor self, Order order)
	{
		base.ResolveOrderInner(self, order);

		DeployNearResources(self, order.Queued);
	}

	public void DeployNearResources(Actor self, bool queued)
	{
		self.QueueActivity(queued, new DeployNearResources(self, OrderLocation));
		self.ShowTargetLines();
		orderLocation = null;
	}

	void ITick.Tick(Actor self)
	{
		if (IsTraitPaused || IsTraitDisabled)
		{
			return;
		}

		ReplenishDeadSlaves(self);
	}

	void INotifySlaveEntering.OnSlaveEntering(Actor self, Actor slave)
	{
		self.QueueActivity(new WaitFor(() => !WaitingForPickup));
	}

	void INotifySlaveEntering.OnSlaveEntered(Actor self, Actor slave)
	{
	}

	void INotifyIdle.TickIdle(Actor self)
	{
		DeployNearResources(self, true);
	}
}
