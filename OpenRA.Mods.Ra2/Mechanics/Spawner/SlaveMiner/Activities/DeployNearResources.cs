using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Extensions;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Activities;

public class DeployNearResources : Activity
{
	readonly MobileMasterMiner masterMiner;
	readonly Mobile mobile;
	readonly Transforms transforms;
	readonly IResourceLayer resourceLayer;
	readonly ResourceClaimLayer claimLayer;
	readonly World world;
	CPos? orderLocation;
	int lastSearchFailed = 1;

	public DeployNearResources(Actor self, CPos? orderLocation = null)
	{
		world = self.World;
		masterMiner = self.TraitOrDefault<MobileMasterMiner>();
		mobile = self.Trait<Mobile>();
		transforms = self.Trait<Transforms>();
		resourceLayer = world.WorldActor.Trait<IResourceLayer>();
		claimLayer = world.WorldActor.Trait<ResourceClaimLayer>();
		this.orderLocation = orderLocation;
	}

	public override bool Tick(Actor self)
	{
		if (IsCanceling || lastSearchFailed > masterMiner.Info.MaxFailedSearches)
			return true;

		if (NextActivity != null)
		{
			return true;
		}

		var closestHarvestableCell = ClosestDeployableLocation(self);
		if (closestHarvestableCell.HasValue)
		{
			orderLocation = closestHarvestableCell;
			claimLayer.TryClaimCell(self, closestHarvestableCell.Value);
			QueueChild(new WaitFor(() => !masterMiner.WaitingForPickup));
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
		var searchFromLoc = orderLocation ?? self.Location;
		if (CanDeployAtLocation(self, searchFromLoc))
		{
			return searchFromLoc;
		}

		var searchRadius = Math.Pow(masterMiner.Info.ScanRadius, lastSearchFailed);
		var path = mobile.PathFinder.FindPathToTargetCellByPredicate(
			self,
			new[] { searchFromLoc },
			loc => CanDeployAtLocation(self, loc),
			BlockedByActor.Stationary,
			loc =>
			{
				var optimalLength = masterMiner.Info.ScanRadius;
				var distance = (loc - searchFromLoc).Length;
				var distanceCost = distance <= optimalLength ?
					masterMiner.Info.CloseDistanceMultiplier * distance :
					masterMiner.Info.FarDistanceMultiplier * distance;

				if (distance > searchRadius)
					return PathGraph.PathCostForInvalidPath;

				if (!CanDeployAtLocation(self, loc))
					return PathGraph.MovementCostForUnreachableCell;

				var resourceDensity = resourceLayer.GetDensityRatioInRadius(
					world, loc, masterMiner.Info.ScanRadius, masterMiner.CanHarvestCell);
				var cost = Math.Round(distanceCost + masterMiner.Info.MaxResourceCost * (1 - resourceDensity));
				if (cost >= masterMiner.Info.MaxResourceCost)
					return PathGraph.PathCostForInvalidPath;

				return (int)cost;
			});

		return path.Count > 0 ? path[0] : null;
	}

	bool CanDeployAtLocation(Actor self, CPos loc)
	{
		if (!masterMiner.CanDeploy(self, loc))
			return false;

		var resourceDensity = resourceLayer.GetDensityRatioInRadius(
			world, loc, masterMiner.Info.ScanRadius, masterMiner.CanHarvestCell);
		if (resourceDensity <= 0)
			return false;

		if (!claimLayer.CanClaimCell(self, loc))
			return false;

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
			yield return new TargetLineNode(Target.FromCell(self.World, orderLocation.Value), masterMiner.Info.DeployLineColor);
	}
}
