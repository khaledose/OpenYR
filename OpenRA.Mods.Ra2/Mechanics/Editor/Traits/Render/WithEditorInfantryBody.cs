using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Editor.Traits.Render;
public class WithEditorInfantryBodyInfo : TraitInfo, IRenderActorPreviewSpritesInfo
{
	[SequenceReference]
	public readonly string Sequence = "stand";

	[PaletteReference(nameof(IsPlayerPalette))]
	[Desc("Custom palette name")]
	public readonly string Palette = null;

	[Desc("Palette is a player palette BaseName")]
	public readonly bool IsPlayerPalette = false;

	public override object Create(ActorInitializer init)
	{
		return new WithEditorInfantryBody();
	}

	public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
	{
		if (string.IsNullOrEmpty(Sequence))
			yield break;

		var anim = new Animation(init.World, image, init.GetFacing());
		anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

		if (IsPlayerPalette)
			p = init.WorldRenderer.Palette(Palette + init.Get<OwnerInit>().InternalName);
		else if (Palette != null)
			p = init.WorldRenderer.Palette(Palette);

		yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
	}
}

public class WithEditorInfantryBody { }
