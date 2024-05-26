using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits;

[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
sealed class GivesVeterancyInfo : TraitInfo
{
	[Desc("If -1, use the value of the unit cost.")]
	public readonly int Experience = -1;

	[Desc("Player relationships the attacking player needs to receive the experience.")]
	public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

	[Desc("Percentage of the `Experience` value that is being granted to the killing actor.")]
	public readonly int ActorExperienceModifier = 100;

	[Desc("Percentage of the `Experience` value that is being granted to the player owning the killing actor.")]
	public readonly int PlayerExperienceModifier = 10;

	public override object Create(ActorInitializer init) { return new GivesVeterancy(this); }
}

sealed class GivesVeterancy : INotifyKilled, INotifyCreated
{
	readonly GivesVeterancyInfo info;

	int exp;
	IEnumerable<int> experienceModifiers;

	public GivesVeterancy(GivesVeterancyInfo info)
	{
		this.info = info;
	}

	void INotifyCreated.Created(Actor self)
	{
		var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
		exp = info.Experience >= 0 ? info.Experience
			: valued != null ? valued.Cost : 0;

		experienceModifiers = self.TraitsImplementing<IGivesExperienceModifier>().ToArray().Select(m => m.GetGivesExperienceModifier());
	}

	void INotifyKilled.Killed(Actor self, AttackInfo e)
	{
		if (exp == 0 || e.Attacker == null || e.Attacker.Disposed)
			return;

		if (!info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
			return;

		exp = Util.ApplyPercentageModifiers(exp, experienceModifiers);

		var killer = e.Attacker.TraitOrDefault<GainsVeterancy>();
		if (killer != null)
		{
			var killerExperienceModifier = e.Attacker.TraitsImplementing<IGainsExperienceModifier>()
				.Select(x => x.GetGainsExperienceModifier()).Append(info.ActorExperienceModifier);
			killer.GiveExperience(Util.ApplyPercentageModifiers(exp, killerExperienceModifier));
		}

		e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()
			?.GiveExperience(Util.ApplyPercentageModifiers(exp, new[] { info.PlayerExperienceModifier }));
	}
}
