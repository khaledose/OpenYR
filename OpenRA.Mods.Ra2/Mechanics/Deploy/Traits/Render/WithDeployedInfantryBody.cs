using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Deploy.Traits.Render;

public class WithDeployedInfantryBodyInfo : ConditionalTraitInfo, Requires<GrantConditionOnDeployInfo>
{
	[SequenceReference]
	[Desc("Sequence to play while deploying")]
	public readonly string DeployingSequence = "deploy";

	[SequenceReference]
	[Desc("Sequence to play while deployed")]
	public readonly string DeployedSequence = "deployed";

	[SequenceReference]
	[Desc("Sequence to play when attacking while deployed")]
	public readonly string DeployedAttackSequence = "deployed-shoot";

	[SequenceReference]
	[Desc("Sequence to play while undeploying. Will reverse the " + nameof(DeployingSequence) + " if not specified")]
	public readonly string UndeployingSequence;

	[Desc("Turret to use while deployed")]
	public readonly string Turret = "deploy";

	[PaletteReference(nameof(IsPlayerPalette))]
	[Desc("Custom palette name")]
	public readonly string Palette = null;

	[Desc("Palette is a player palette BaseName")]
	public readonly bool IsPlayerPalette = false;

	public override object Create(ActorInitializer init) { return new WithDeployedInfantryBody(init, this); }
}

public class WithDeployedInfantryBody : ConditionalTrait<WithDeployedInfantryBodyInfo>, INotifyAttack, INotifyDeployTriggered, INotifyDeployComplete
{
	IEnumerable<INotifyDeployComplete> notify;
	protected readonly Animation DefaultAnimation;

	public WithDeployedInfantryBody(ActorInitializer init, WithDeployedInfantryBodyInfo info)
		: base(info)
	{
		var self = init.Self;
		var rs = self.Trait<RenderSprites>();
		var t = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == info.Turret);
		var facingFunc = t is null ? RenderSprites.MakeFacingFunc(self) : () => t.WorldOrientation.Yaw;

		DefaultAnimation = new Animation(init.World, rs.GetImage(self), facingFunc);
		PlayDeployedAnimation(self);
		rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled), info.Palette, info.IsPlayerPalette);

		if (t is not null)
		{
			t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		notify = self.TraitsImplementing<INotifyDeployComplete>();
	}

	void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		=> self.World.AddFrameEndTask(_ => Attacking(self));

	void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

	void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		=> self.World.AddFrameEndTask(_ => Deploy(self));

	void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim)
		=> self.World.AddFrameEndTask(_ => Undeploy(self));

	void INotifyDeployComplete.FinishedDeploy(Actor self)
		=> self.World.AddFrameEndTask(_ => PlayDeployedAnimation(self));

	void INotifyDeployComplete.FinishedUndeploy(Actor self) { }

	protected void Deploy(Actor self)
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
		}
	}

	protected void PlayDeployedAnimation(Actor self)
	{
		var sequence = Info.DeployedSequence;

		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayRepeating(normalized);
		}
	}

	protected void Undeploy(Actor self)
	{
		var sequence = Info.UndeployingSequence;
		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayThen(normalized, () =>
			{
				foreach (var n in notify)
				{
					n.FinishedUndeploy(self);
				}
			});

			return;
		}

		sequence = Info.DeployingSequence;
		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayBackwardsThen(normalized, () =>
			{
				foreach (var n in notify)
				{
					n.FinishedUndeploy(self);
				}
			});
		}
	}

	protected void Attacking(Actor self)
	{
		var sequence = Info.DeployedAttackSequence;

		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.PlayThen(normalized, () => PlayDeployedAnimation(self));
		}
	}

	public string NormalizeSequence(Actor self, string sequence)
		=> RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
}
