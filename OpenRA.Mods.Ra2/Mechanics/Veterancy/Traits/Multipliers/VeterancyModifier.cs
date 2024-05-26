using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits.Multipliers;

[Desc("Unit veterancy.")]
public class VeterancyModifierInfo : TraitInfo, Requires<GainsVeterancyInfo>
{
	[Desc("Percentual damage per veterancy level.")]
	public readonly int[] DamageRates = Array.Empty<int>();

	[Desc("Percentual strength per veterancy level.")]
	public readonly int[] StrengthRates = Array.Empty<int>();

	[Desc("Percentual inaccuracy per veterancy level.")]
	public readonly int[] InaccuracyRates = Array.Empty<int>();

	[Desc("Percentual range per veterancy level.")]
	public readonly int[] RangeRates = Array.Empty<int>();

	[Desc("Percentual reload times per veterancy level.")]
	public readonly int[] ReloadRates = Array.Empty<int>();

	[Desc("Percentual speed per veterancy level.")]
	public readonly int[] SpeedRates = Array.Empty<int>();

	[Desc("Percentual sight per veterancy level.")]
	public readonly int[] SightRates = Array.Empty<int>();

	[Desc("Percentual experience to be given per veterancy level.")]
	public readonly int[] ExperienceRates = Array.Empty<int>();

	[Desc("Self heal per veterancy level.")]
	public readonly int[] HealRates = Array.Empty<int>();

	[Desc("Delay in ticks between healing.")]
	public readonly int HealDelay = 1;

	[Desc("Apply the selfhealing using these damagetypes.")]
	public readonly BitSet<DamageType> DamageTypes;

	public override object Create(ActorInitializer init)
	{
		return new VeterancyModifier(init, this);
	}
}

public class VeterancyModifier : IFirepowerModifier, IDamageModifier, IInaccuracyModifier,
	IRangeModifier, IReloadModifier, ISpeedModifier, IRevealsShroudModifier, IGivesExperienceModifier, ITick
{
	readonly VeterancyModifierInfo info;
	readonly Health health;
	readonly GainsVeterancy veterancy;

	int CurrentLevel => veterancy.Level;

	public VeterancyModifier(ActorInitializer init, VeterancyModifierInfo info)
	{
		this.info = info;
		health = init.Self.TraitOrDefault<Health>();
		veterancy = init.Self.TraitOrDefault<GainsVeterancy>();
	}

	int IFirepowerModifier.GetFirepowerModifier()
	{
		return GetMaxPossibleModifier(info.DamageRates);
	}

	int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
	{
		return GetMaxPossibleModifier(info.StrengthRates);
	}

	int IInaccuracyModifier.GetInaccuracyModifier()
	{
		return GetMaxPossibleModifier(info.InaccuracyRates);
	}

	int IRangeModifier.GetRangeModifier()
	{
		return GetMaxPossibleModifier(info.RangeRates);
	}

	int IReloadModifier.GetReloadModifier()
	{
		return GetMaxPossibleModifier(info.ReloadRates);
	}

	int ISpeedModifier.GetSpeedModifier()
	{
		return GetMaxPossibleModifier(info.SpeedRates);
	}

	int IRevealsShroudModifier.GetRevealsShroudModifier()
	{
		return GetMaxPossibleModifier(info.SightRates);
	}

	int IGivesExperienceModifier.GetGivesExperienceModifier()
	{
		return GetMaxPossibleModifier(info.ExperienceRates);
	}

	void ITick.Tick(Actor self)
	{
		if (info.HealRates.Length == 0 || CurrentLevel == 0)
			return;

		if (self.CurrentActivity == null && self.World.WorldTick % info.HealDelay == 0)
			health.InflictDamage(self, self, new(-GetMaxPossibleModifier(info.HealRates), info.DamageTypes), true);
	}

	int GetMaxPossibleModifier(int[] modifiers)
	{
		var level = Math.Min(CurrentLevel, modifiers.Length);
		return level == 0 ? 100 : modifiers[level - 1];
	}
}
