using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits;

public class VeterancyOnProductionInfo : ConditionalTraitInfo, Requires<GainsVeterancyInfo>
{
	public readonly string[] Prerequisites = Array.Empty<string>();

	[Desc("Only grant this level for certain factions.")]
	public readonly HashSet<string> Factions = new();

	[Desc("Should it recheck everything when it is captured?")]
	public readonly bool ResetOnOwnerChange = false;

	[Desc("Number of levels to give to the actor on creation.")]
	public readonly int InitialRank = 1;

	[Desc("Should the level-up animation be suppressed when actor is created?")]
	public readonly bool SuppressLevelupAnimation = true;

	public override object Create(ActorInitializer init) { return new VeterancyOnProduction(init, this); }
}

public class VeterancyOnProduction : ConditionalTrait<VeterancyOnProductionInfo>, INotifyOwnerChanged
{
	readonly VeterancyOnProductionInfo info;
	string faction;

	public VeterancyOnProduction(ActorInitializer init, VeterancyOnProductionInfo info)
		: base(info)
	{
		this.info = info;
		faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
	}

	public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
	{
		if (info.ResetOnOwnerChange)
			faction = newOwner.Faction.InternalName;
	}

	protected override void Created(Actor self)
	{
		if (IsTraitDisabled)
			return;

		if (info.Factions.Count > 0 && !info.Factions.Contains(faction))
			return;

		if (info.Prerequisites.Length > 0 && !self.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.Prerequisites))
			return;

		var gv = self.Trait<GainsVeterancy>();
		if (gv?.CanGainLevel != true)
			return;

		gv.GiveLevels(info.InitialRank, info.SuppressLevelupAnimation);
	}
}
