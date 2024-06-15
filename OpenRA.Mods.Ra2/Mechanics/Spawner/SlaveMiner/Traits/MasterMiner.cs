using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.SlaveMiner;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Interfaces;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;

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

public abstract class MasterMiner : SpawnerMaster, INotifySlaveMinerTransformed, INotifyTransform, IIssueOrder, IResolveOrder
{
	public new readonly MasterMinerInfo Info;
	protected new readonly List<MinerLinkedSlave> LinkedSlaves = new();
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
		: base(info)
	{
		Info = info;
		World = init.Self.World;
		Transforms = init.Self.TraitOrDefault<Transforms>();
		ResourceLayer = World.WorldActor.Trait<IResourceLayer>();
	}

	protected override void CreateSlave(Actor self, LinkedSlave linkedSlave)
	{
		base.CreateSlave(self, linkedSlave);


		OnSlaveCreated(self, linkedSlave as MinerLinkedSlave);	
	}

	protected override void OnSlaveCreated(Actor self, LinkedSlave linkedSlave)
	{
		base.OnSlaveCreated(self, linkedSlave);
		if (linkedSlave is MinerLinkedSlave minerLinkedSlave)
		{
			minerLinkedSlave.MasterTransformed = linkedSlave.Actor
			 .TraitsImplementing<INotifySlaveMinerTransformed>()
			 .FirstOrDefault();
		}
	}

	void INotifySlaveMinerTransformed.OnTransformCompleted(Actor self, MasterMiner masterMiner)
	{
		OnTransformCompletedInner(self, masterMiner);
	}

	void INotifyTransform.BeforeTransform(Actor self) { }

	void INotifyTransform.OnTransform(Actor self) { }

	void INotifyTransform.AfterTransform(Actor toActor)
	{
		AfterTransformInner(toActor);
	}

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

	public virtual bool CanHarvestCell(CPos cell)
	{
		if (cell.Layer != 0)
			return false;

		var resourceType = ResourceLayer.GetResource(cell).Type;
		if (resourceType == null)
			return false;

		return Info.Resources.Contains(resourceType);
	}

	public virtual int GetResourcesDensity(CPos location)
	{
		var totalDensity = 0;
		foreach (var tile in World.Map.FindTilesInCircle(location, Info.ScanRadius))
		{
			if (CanHarvestCell(tile))
			{
				totalDensity += ResourceLayer.GetResource(tile).Density;
			}
		}

		return totalDensity;
	}

	protected virtual void AfterTransformInner(Actor newMaster)
	{
		var masterMiner = newMaster.TraitOrDefault<MasterMiner>();
		if (masterMiner is null)
		{
			return;
		}

		var aliveSlaves = LinkedSlaves.Where(s => s.IsAlive);
		foreach (var slave in aliveSlaves)
		{
			slave.SlaveLinked.Link(slave.Actor, newMaster);
			slave.MasterTransformed.OnTransformCompleted(slave.Actor, masterMiner);
		}

		newMaster
			.TraitsImplementing<INotifySlaveMinerTransformed>()
			.FirstOrDefault().OnTransformCompleted(newMaster, this);
		LinkedSlaves.Clear();
	}

	protected virtual void OnTransformCompletedInner(Actor self, MasterMiner masterMiner)
	{
		LinkedSlaves.Clear();
		LinkedSlaves.AddRange(masterMiner.LinkedSlaves);
		orderLocation = masterMiner.orderLocation;
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
