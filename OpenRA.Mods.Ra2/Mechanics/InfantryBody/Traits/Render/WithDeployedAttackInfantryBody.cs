using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Ra2.Mechanics.Deploy.Traits.Render;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.Render;
public class WithDeployedAttackInfantryBodyInfo : WithDeployedInfantryBodyInfo
{
	[SequenceReference]
	[Desc("Sequence to play when attacking while deployed")]
	public readonly string AttackSequence = "deployed-shoot";

	[Desc("Turret to use while deployed")]
	public readonly string Turret = "deploy";

	public override object Create(ActorInitializer init) { return new WithDeployedAttackInfantryBody(init, this); }
}

public class WithDeployedAttackInfantryBody : WithDeployedInfantryBody, INotifyAttack
{
	protected new readonly WithDeployedAttackInfantryBodyInfo Info;
	readonly Turreted turret;

	public WithDeployedAttackInfantryBody(ActorInitializer init, WithDeployedAttackInfantryBodyInfo info)
		: base(init, info)
	{
		Info = info;
		turret = init.Self.TraitsImplementing<Turreted>()
			.FirstOrDefault(t => t.Name == Info.Turret);
		if (turret is null)
			return;

		turret.QuantizedFacings = DefaultAnimation.GetSequence(Info.AttackSequence).Facings;
	}

	protected override Func<WAngle> SetFacingFunc(Actor self)
	{
		return () =>
		{
			if (turret is null) return WAngle.Zero;

			return turret.WorldOrientation.Yaw;
		};
	}

	void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		=> self.World.AddFrameEndTask(_ => PlayAttackAnimation(self));

	void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

	public virtual void PlayAttackAnimation(Actor self)
	{
		var sequence = Info.AttackSequence;

		if (!string.IsNullOrEmpty(sequence))
		{
			var normalized = NormalizeSequence(self, sequence);
			DefaultAnimation.Play(normalized);
		}
	}
}
