using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits.Render;

public class VeterancyDecorationInfo : WithDecorationBaseInfo, Requires<GainsVeterancyInfo>
{
	[Desc("Image used for this decoration. Defaults to the actor's type.")]
	public readonly string Image = null;

	[FieldLoader.Require]
	[SequenceReference(nameof(Image), allowNullImage: true)]
	[Desc("Sequence used for this decoration (can be animated).")]
	public readonly string[] SequencePerLevel = null;

	[PaletteReference(nameof(IsPlayerPalette))]
	[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
	public readonly string Palette = "chrome";

	[Desc("Custom palette is a player palette BaseName")]
	public readonly bool IsPlayerPalette = false;

	public override object Create(ActorInitializer init) { return new VeterancyDecoration(init.Self, this); }
}

public class VeterancyDecoration : WithDecorationBase<VeterancyDecorationInfo>, ITick, INotifyVeterancyRankUp
{
	readonly VeterancyDecorationInfo info;
	protected Animation anim;
	readonly string image;
	readonly GainsVeterancy veterancy;

	public VeterancyDecoration(Actor self, VeterancyDecorationInfo info)
		: base(self, info)
	{
		this.info = info;
		veterancy = self.Trait<GainsVeterancy>();
		image = info.Image ?? self.Info.Name;
	}

	protected virtual PaletteReference GetPalette(Actor self, WorldRenderer wr)
	{
		return wr.Palette(Info.IsPlayerPalette ? Info.Palette + self.Owner.InternalName : Info.Palette);
	}

	protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
	{
		if (anim == null)
			return Enumerable.Empty<IRenderable>();

		return new IRenderable[]
		{
			new UISpriteRenderable(anim.Image, self.CenterPosition, screenPos - (0.5f * anim.Image.Size.XY).ToInt2(), 0, GetPalette(self, wr))
		};
	}

	void ITick.Tick(Actor self)
	{
		if (veterancy.Level == 0)
			return;

		anim.Tick();
	}

	void INotifyVeterancyRankUp.OnRankUp(Actor self)
	{
		var level = Math.Min(veterancy.Level, info.SequencePerLevel.Length);

		anim = new Animation(self.World, image, () => self.World.Paused);
		anim.PlayRepeating(info.SequencePerLevel[level - 1]);
	}
}
