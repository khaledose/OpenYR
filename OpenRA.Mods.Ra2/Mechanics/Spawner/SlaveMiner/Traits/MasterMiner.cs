using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Mechanics.SlaveMiner.Activities;
using OpenRA.Mods.RA2.Mechanics.SlaveMiner.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Master.Traits;

public class MasterMinerInfo : SpawnerMasterInfo, Requires<TransformsInfo>
{
	[Desc("Radius used to scan for nearby resources.")]
	public readonly int ScanRadius = 2;

	[Desc("Delay between each resource scan.")]
	public readonly int ScanDelay = 20;

	[Desc("Which resources it can harvest.")]
	public readonly HashSet<string> Resources = new();

	[CursorReference]
	[Desc("Cursor to display when ordering to harvest resources.")]
	public readonly string HarvestCursor = "harvest";

	public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
	{
		base.RulesetLoaded(rules, ai);

		if (Resources.Count == 0)
		{
			var slaveInfo = rules.Actors[Actors.FirstOrDefault()];
			var harvesterInfo = slaveInfo.TraitInfoOrDefault<HarvesterInfo>();
			foreach (var res in harvesterInfo.Resources)
			{
				Resources.Add(res);
			}
		}
	}

	public ActorInfo GetTransformsActorInfo(Ruleset rules, ActorInfo ai)
	{
		var transformsInfo = ai.TraitInfoOrDefault<TransformsInfo>();
		return rules.Actors[transformsInfo.IntoActor];
	}
}

public abstract class MasterMiner : SpawnerMaster, INotifyActorDisposing, INotifyTransform, IIssueOrder, IResolveOrder
{
	public new readonly MasterMinerInfo Info;
	public CPos? OrderLocation => orderLocation;

	protected readonly Transforms Transforms;
	protected readonly IResourceLayer ResourceLayer;
	protected readonly World World;
	protected CPos? orderLocation;
	IEnumerable<IOrderTargeter> IIssueOrder.Orders
	{
		get
		{
			if (IsTraitDisabled || IsTraitPaused)
				yield break;

			yield return new DeployNearResourcesTargeter();
		}
	}

	protected MasterMiner(ActorInitializer init, MasterMinerInfo info)
		: base(init, info)
	{
		Info = info;
		World = init.Self.World;
		Transforms = init.Self.TraitOrDefault<Transforms>();
		ResourceLayer = World.WorldActor.Trait<IResourceLayer>();
	}

	void INotifyTransform.BeforeTransform(Actor self) { }

	void INotifyTransform.OnTransform(Actor self) { }

	void INotifyTransform.AfterTransform(Actor toActor)
	{
		AfterTransformInner(toActor);
	}

	void INotifyActorDisposing.Disposing(Actor self) { }

	Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
	{
		if (order.OrderID == "Harvest")
		{
			orderLocation = self.World.Map.CellContaining(target.CenterPosition);
			return new Order(order.OrderID, self, target, queued);
		}

		return null;
	}

	void IResolveOrder.ResolveOrder(Actor self, Order order)
	{
		if (order.OrderString == "Harvest")
		{
			ResolveOrderInner(self, order);
		}
	}

	protected virtual void ReplenishDeadSlaves(Actor self)
	{
		foreach (var slaveEntry in SlaveEntries.Where(s => !s.IsValid))
		{
			Replenish(self, slaveEntry);
		}
	}

	public virtual bool CanSlavesHarvestCell(CPos cell)
	{
		if (cell.Layer != 0)
			return false;

		var resourceType = ResourceLayer.GetResource(cell).Type;
		if (resourceType == null)
			return false;

		return Info.Resources.Contains(resourceType);
	}

	public virtual int GetResourceDensityAtLocation(CPos location)
	{
		var totalDensity = 0;
		foreach (var tile in World.Map.FindTilesInCircle(location, Info.ScanRadius))
		{
			if (CanSlavesHarvestCell(tile))
			{
				totalDensity += ResourceLayer.GetResource(tile).Density;
			}
		}

		return totalDensity;
	}

	public virtual void OnTransformCompleted(MasterMiner oldMasterMiner)
	{
		SlaveEntries = oldMasterMiner.SlaveEntries;
		orderLocation = oldMasterMiner.orderLocation;
	}

	protected virtual void AfterTransformInner(Actor newMaster)
	{
		var masterMiner = newMaster.TraitOrDefault<MasterMiner>();
		if (masterMiner is null)
		{
			return;
		}

		foreach (var slaveEntry in SlaveEntries.Where(s => s.IsValid))
		{
			slaveEntry.SpawnerSlave.LinkMaster(slaveEntry.Actor, newMaster, masterMiner);
			var notifySlave = slaveEntry.Actor.TraitsImplementing<INotifyMasterTransformed>().FirstEnabledTraitOrDefault();
			notifySlave.OnMasterTransformed(slaveEntry.Actor);
		}

		masterMiner.OnTransformCompleted(this);
		SlaveEntries = Array.Empty<BaseSpawnerSlaveEntry>();
	}

	protected virtual void ResolveOrderInner(Actor self, Order order)
	{
		CPos? loc = null;
		if (order.Target.Type != TargetType.Invalid)
		{
			loc = self.World.Map.CellContaining(order.Target.CenterPosition);
		}

		orderLocation = loc ?? self.Location;
	}
}
