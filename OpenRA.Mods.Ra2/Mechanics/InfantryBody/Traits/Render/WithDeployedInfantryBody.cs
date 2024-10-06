using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Deploy.Traits.Render;

public class WithDeployedInfantryBodyInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<DeployableInfo>
{
	[SequenceReference]
	[Desc("Sequence to play while deploying")]
	public readonly string DeployingSequence = "deploy";

	[SequenceReference]
	[Desc("Sequence to play while deployed")]
	public readonly string DeployedSequence = "deployed";

	[SequenceReference]
	[Desc("Sequence to play while undeploying. Will reverse the " + nameof(DeployingSequence) + " if not specified")]
	public readonly string UndeployingSequence;

	[PaletteReference(nameof(IsPlayerPalette))]
	[Desc("Custom palette name")]
	public readonly string Palette = null;

	[Desc("Palette is a player palette BaseName")]
	public readonly bool IsPlayerPalette = false;

	public override object Create(ActorInitializer init) { return new WithDeployedInfantryBody(init, this); }
}

public class WithDeployedInfantryBody : ConditionalTrait<WithDeployedInfantryBodyInfo>, INotifyDeployTriggered, INotifyDeployComplete
{
	readonly IEnumerable<INotifyDeployedInfantryBodyAnimating> animNotifications;
	IEnumerable<INotifyDeployComplete> notify;
	public readonly Animation DefaultAnimation;
	protected readonly Func<WAngle> FacingFunc;

	public WithDeployedInfantryBody(ActorInitializer init, WithDeployedInfantryBodyInfo info)
		: base(info)
	{
		var self = init.Self;
		animNotifications = self.TraitsImplementing<INotifyDeployedInfantryBodyAnimating>();
		FacingFunc = SetFacingFunc(self);
		DefaultAnimation = InitializeAnimation(self);
		PlayDeployedAnimation(self);
	}

	protected virtual Animation InitializeAnimation(Actor self)
	{
		var renderSprites = self.Trait<RenderSprites>();

		var animation = new Animation(
			self.World,
			renderSprites.GetImage(self),
			FacingFunc);

		renderSprites.Add(new AnimationWithOffset(animation, null, () => IsTraitDisabled),
			Info.Palette, Info.IsPlayerPalette);

		return animation;
	}

	protected virtual Func<WAngle> SetFacingFunc(Actor self)
	{
		return RenderSprites.MakeFacingFunc(self);
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		notify = self.TraitsImplementing<INotifyDeployComplete>();
	}

	void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		=> self.World.AddFrameEndTask(_ => PlayDeployingAnimation(self));

	void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim)
		=> self.World.AddFrameEndTask(_ => PlayUndeployingAnimation(self));

	void INotifyDeployComplete.FinishedDeploy(Actor self)
		=> self.World.AddFrameEndTask(_ => PlayDeployedAnimation(self));

	void INotifyDeployComplete.FinishedUndeploy(Actor self) { }

	public virtual void PlayDeployingAnimation(Actor self)
	{
		var sequence = Info.DeployingSequence;
		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayThen(normalized, () =>
			{
				foreach (var n in notify)
				{
					n.FinishedDeploy(self);
				}
			});

			foreach (var notif in animNotifications)
			{
				notif.OnDeployingAnimation(self);
			}
		}
	}

	public virtual void PlayDeployedAnimation(Actor self)
	{
		var sequence = Info.DeployedSequence;

		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayRepeating(normalized);

			foreach (var notif in animNotifications)
			{
				notif.OnDeployedAnimation(self);
			}
		}
	}

	public virtual void PlayUndeployingAnimation(Actor self)
	{
		var sequence = Info.UndeployingSequence;

		if (!string.IsNullOrEmpty(sequence))
		{
			PlaySequence(self, sequence, false);
			return;
		}

		sequence = Info.DeployingSequence;
		if (!string.IsNullOrEmpty(sequence))
		{
			PlaySequence(self, sequence, true);
		}
	}

	public string NormalizeSequence(Actor self, string sequence)
		=> RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);

	void PlaySequence(Actor self, string sequence, bool playBackwards)
	{
		var normalized = NormalizeSequence(self, sequence);
		if (playBackwards)
		{
			DefaultAnimation.PlayBackwardsThen(normalized, () => OnUndeployingFinished(self));
		}
		else
		{
			DefaultAnimation.PlayThen(normalized, () => OnUndeployingFinished(self));
		}

		foreach (var notif in animNotifications)
		{
			notif.OnUndeployingAnimation(self);
		}
	}

	void OnUndeployingFinished(Actor self)
	{
		foreach (var n in notify)
		{
			n.FinishedUndeploy(self);
		}
	}
}
