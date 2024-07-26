﻿using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Deploy.Traits.Render;

public class WithSlaveBodyInfo : ConditionalTraitInfo, Requires<HarvesterInfo>
{
	[SequenceReference]
	[Desc("Sequence to play when attacking while deployed")]
	public readonly string HarvestingSequence = "harvest";

	[GrantedConditionReference]
	[Desc("The condition to grant after harvesting.")]
	public readonly string HarvestingCondition = null;

	[PaletteReference(nameof(IsPlayerPalette))]
	[Desc("Custom palette name")]
	public readonly string Palette = null;

	[Desc("Palette is a player palette BaseName")]
	public readonly bool IsPlayerPalette = false;

	public override object Create(ActorInitializer init) { return new WithSlaveBody(init, this); }
}

public class WithSlaveBody : ConditionalTrait<WithSlaveBodyInfo>, INotifyHarvestAction
{
	protected readonly Animation DefaultAnimation;
	int token = Actor.InvalidConditionToken;

	public WithSlaveBody(ActorInitializer init, WithSlaveBodyInfo info)
		: base(info)
	{
		var self = init.Self;
		var rs = self.Trait<RenderSprites>();

		DefaultAnimation = new Animation(init.World, rs.GetImage(self));
		Harvesting(self);
		rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled), info.Palette, info.IsPlayerPalette);
	}

	protected void Harvesting(Actor self)
	{
		if (token == Actor.InvalidConditionToken)
			token = self.GrantCondition(Info.HarvestingCondition);

		var sequence = Info.HarvestingSequence;

		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayThen(normalized, () => token = self.RevokeCondition(token));
		}
	}

	public string NormalizeSequence(Actor self, string sequence)
		=> RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);

	public void Harvested(Actor self, string resourceType)
		=> self.World.AddFrameEndTask(_ => Harvesting(self));

	public void MovingToResources(Actor self, CPos targetCell){}

	public void MovementCancelled(Actor self){}
}
