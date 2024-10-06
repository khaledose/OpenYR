using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.Render;

public class WithInfantrySequenceModifierInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
{
	[SequenceReference(prefix: true)]
	[FieldLoader.Require]
	[Desc("Sequence prefix to apply while trait enabled.")]
	public readonly string SequencePrefix;

	public override object Create(ActorInitializer init)
	{
		return new WithInfantrySequenceModifier(this);
	}
}

public class WithInfantrySequenceModifier : ConditionalTrait<WithInfantrySequenceModifierInfo>, IRenderInfantrySequenceModifier
{
	public WithInfantrySequenceModifier(WithInfantrySequenceModifierInfo info)
		: base(info)
	{
	}

	public bool IsModifyingSequence => !IsTraitDisabled;

	public string SequencePrefix => Info.SequencePrefix;
}
