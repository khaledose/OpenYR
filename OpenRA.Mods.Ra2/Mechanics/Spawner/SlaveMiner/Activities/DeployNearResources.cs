using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Activities;

public class DeployNearResources : Activity
{
	readonly MobileMasterMiner masterMiner;
	readonly MobileMasterMinerInfo masterMinerInfo;
	readonly ActorInfo masterActor;
	readonly BuildingInfo buildingInfo;
	readonly Mobile mobile;
	readonly Transforms transforms;
	readonly World world;
	CPos? orderLocation;
	bool hasWaited;
	int lastSearchFailed = 1;

	public DeployNearResources(Actor self, CPos? orderLocation = null)
	{
		world = self.World;
		masterMiner = self.TraitOrDefault<MobileMasterMiner>();
		masterMinerInfo = masterMiner.Info;
		mobile = self.Trait<Mobile>();
		transforms = self.Trait<Transforms>();
		masterActor = world.Map.Rules.Actors[transforms.Info.IntoActor];
		buildingInfo = masterActor.TraitInfoOrDefault<BuildingInfo>();
		this.orderLocation = orderLocation;
	}

	public override bool Tick(Actor self)
	{
		if (IsCanceling)
			return true;

		if (NextActivity != null)
		{
			return true;
		}

		if (masterMiner.WaitingForPickup)
		{
			QueueChild(new WaitFor(() => !masterMiner.WaitingForPickup));
			return false;
		}

		if (lastSearchFailed > 1 && !hasWaited)
		{
			QueueChild(new Wait(masterMiner.Info.ScanDelay));
			hasWaited = true;
			return false;
		}

		hasWaited = false;

		var closestHarvestableCell = ClosestDeployableLocation(self);

		if (closestHarvestableCell.HasValue)
		{
			orderLocation = closestHarvestableCell;
			QueueChild(mobile.MoveTo(closestHarvestableCell.Value));
			QueueChild(transforms.GetTransformActivity());
			lastSearchFailed = 1;
			return false;
		}

		lastSearchFailed++;
		return false;
	}

	CPos? ClosestDeployableLocation(Actor self)
	{
		if (CanDeployAtLocation(self, orderLocation ?? self.Location))
		{
			return orderLocation ?? self.Location;
		}

		var searchFromLoc = orderLocation ?? self.Location;
		var searchRadius = Math.Pow(masterMinerInfo.ScanRadius, lastSearchFailed);
		var path = mobile.PathFinder.FindPathToTargetCellByPredicate(
			self,
			new[] { searchFromLoc },
			loc => CanDeployAtLocation(self, loc),
			BlockedByActor.Immovable,
			loc =>
			{
				var length = (loc - self.Location).LengthSquared;
				if (length > searchRadius)
					return PathGraph.PathCostForInvalidPath;

				if (!CanDeployAtLocation(self, loc))
					return PathGraph.MovementCostForUnreachableCell;

				var resourceDensity = masterMiner.GetResourcesDensity(loc);
				var weight = length - resourceDensity;
				return Math.Max(weight, 0);
			});

		if (path.Count > 0)
			return path[0];

		return null;
	}

	bool CanDeployAtLocation(Actor self, CPos location)
	{
		if (transforms.IsTraitPaused || transforms.IsTraitDisabled)
		{
			return false;
		}

		if (buildingInfo != null && !world.CanPlaceBuilding(location, masterActor, buildingInfo, self))
		{
			return false;
		}

		var resourceDensity = masterMiner.GetResourcesDensity(location);
		if (resourceDensity <= 0)
		{
			return false;
		}

		return true;
	}

	public override IEnumerable<Target> GetTargets(Actor self)
	{
		yield return Target.FromCell(self.World, self.Location);
	}

	public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
	{
		if (ChildActivity != null)
			foreach (var n in ChildActivity.TargetLineNodes(self))
				yield return n;

		if (orderLocation != null)
			yield return new TargetLineNode(Target.FromCell(self.World, orderLocation.Value), masterMinerInfo.DeployLineColor);
	}
}
