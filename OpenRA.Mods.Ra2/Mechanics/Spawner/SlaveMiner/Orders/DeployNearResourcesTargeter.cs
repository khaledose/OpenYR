using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Orders;

public class DeployNearResourcesTargeter : IOrderTargeter
{
	public string OrderID => "Harvest";
	public int OrderPriority => 10;
	public bool IsQueued { get; private set; }
	public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
	{
		return true;
	}

	public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
	{
		if (target.Type != TargetType.Terrain)
			return false;

		if (modifiers.HasModifier(TargetModifiers.ForceMove))
			return false;

		var location = self.World.Map.CellContaining(target.CenterPosition);

		// Don't leak info about resources under the shroud
		if (!self.Owner.Shroud.IsExplored(location))
			return false;

		var info = self.Info.TraitInfo<MasterMinerInfo>();
		var res = self.World.WorldActor.TraitsImplementing<IResourceRenderer>()
			.Select(r => r.GetRenderedResourceType(location))
			.FirstOrDefault(r => r != null && info.Resources.Contains(r));

		if (res == null)
			return false;

		cursor = info.HarvestCursor;
		IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

		return true;
	}
}
