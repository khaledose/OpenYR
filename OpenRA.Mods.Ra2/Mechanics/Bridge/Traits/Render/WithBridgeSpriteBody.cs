using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Interfaces;
using OpenRA.Mods.Ra2.Mechanics.Sequence.Traits;
using OpenRA.Traits;
using System.Xml.Linq;

namespace OpenRA.Mods.Ra2.Mechanics.Bridge.Traits.Render;

public class WithBridgeSpriteBodyInfo : WithSpriteBodyInfo, Requires<SequenceNormalizerInfo>
{
	public readonly string PrevPrefix = "prev-";
	public readonly string NextPrefix = "next-";
	public override object Create(ActorInitializer init) { return new WithBridgeSpriteBody(init, this); }

	public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
	{
		if (!EnabledByDefault)
			yield break;

		var anim = new Animation(init.World, image);
		anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => 0);

		yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
	}
}

public class WithBridgeSpriteBody : WithSpriteBody, INotifyBridgeNodeAttacked
{
	protected readonly new WithBridgeSpriteBodyInfo Info;
	readonly SequenceNormalizer normalizer;

	public WithBridgeSpriteBody(ActorInitializer init, WithBridgeSpriteBodyInfo info)
		: base(init, info)
	{
		Info = info;
		normalizer = init.Self.Trait<SequenceNormalizer>();
	}

	protected override void DamageStateChanged(Actor self)
	{
		DefaultAnimation.ReplaceAnim(normalizer.NormalizeSequence(DefaultAnimation, Info.Sequence, self.GetDamageState()));
	}

	void INotifyBridgeNodeAttacked.OnPrevNodeDamaged(IBridgeNode node)
	{
		var sequence = (node.Direction == BridgeDirection.Positive ? Info.PrevPrefix : Info.NextPrefix) + Info.Sequence;
		DefaultAnimation.ReplaceAnim(normalizer.NormalizeSequence(DefaultAnimation, sequence, node.Actor.GetDamageState()));
	}

	void INotifyBridgeNodeAttacked.OnNextNodeDamaged(IBridgeNode node)
	{
		var sequence = (node.Direction == BridgeDirection.Negative ? Info.PrevPrefix : Info.NextPrefix) + Info.Sequence;
		DefaultAnimation.ReplaceAnim(normalizer.NormalizeSequence(DefaultAnimation, sequence, node.Actor.GetDamageState()));
	}
}
