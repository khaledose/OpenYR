using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits.Conditions;

[Desc("Grant condition to this actor when gains a new veterancy level.")]
public class GrantConditionOnVeterancyLevelInfo : TraitInfo, Requires<GainsVeterancyInfo>
{
	[FieldLoader.Require]
	[GrantedConditionReference]
	[Desc("Condition granted for level")]
	public readonly string Condition = null;

	[Desc("Veterancy level to grant the condition for")]
	public readonly int Level = 1;

	public override object Create(ActorInitializer init) { return new GrantConditionOnVeterancyLevel(init, this); }
}

public class GrantConditionOnVeterancyLevel : INotifyCreated, INotifyVeterancyRankUp
{
	readonly GrantConditionOnVeterancyLevelInfo info;
	GainsVeterancy veterancy;
	int conditionToken = Actor.InvalidConditionToken;

	public GrantConditionOnVeterancyLevel(ActorInitializer init, GrantConditionOnVeterancyLevelInfo info)
	{
		this.info = info;
	}

	void INotifyCreated.Created(Actor self)
	{
		veterancy = self.TraitOrDefault<GainsVeterancy>();
	}

	void INotifyVeterancyRankUp.OnRankUp(Actor self)
	{
		if (conditionToken != Actor.InvalidConditionToken)
			return;

		if (veterancy is not null && veterancy.Level == info.Level)
		{
			conditionToken = self.GrantCondition(info.Condition);
		}
	}
}
