using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.Render;

public class WithInfantryBodyInfo : ConditionalTraitInfo, Requires<IMoveInfo>, Requires<RenderSpritesInfo>
{
	public readonly int MinIdleDelay = 30;
	public readonly int MaxIdleDelay = 110;

	[SequenceReference]
	public readonly string MoveSequence = "run";

	[SequenceReference]
	public readonly string DefaultAttackSequence = null;

	[SequenceReference(dictionaryReference: LintDictionaryReference.Values)]
	[Desc("Attack sequence to use for each armament.",
		"A dictionary of [armament name]: [sequence name(s)].",
		"Multiple sequence names can be defined to specify per-burst animations.")]
	public readonly Dictionary<string, string[]> AttackSequences = new();

	[SequenceReference]
	public readonly string[] IdleSequences = Array.Empty<string>();

	[SequenceReference]
	public readonly string[] StandSequences = { "stand" };

	[PaletteReference(nameof(IsPlayerPalette))]
	[Desc("Custom palette name")]
	public readonly string Palette = null;

	[Desc("Palette is a player palette BaseName")]
	public readonly bool IsPlayerPalette = false;

	public override object Create(ActorInitializer init) { return new WithInfantryBody(init, this); }
}

public class WithInfantryBody : ConditionalTrait<WithInfantryBodyInfo>, ITick, INotifyAttack, INotifyIdle
{
	readonly IMove move;
	public readonly Animation DefaultAnimation;

	bool dirty;
	string idleSequence;
	int idleDelay;
	protected AnimationState state;
	protected AnimationState previousState;

	IRenderInfantrySequenceModifier rsm;
	readonly IEnumerable<INotifyInfantryBodyAnimating> animNotifications;

	bool IsModifyingSequence => rsm != null && rsm.IsModifyingSequence;
	bool wasModifying;

	public WithInfantryBody(ActorInitializer init, WithInfantryBodyInfo info)
		: base(info)
	{
		var self = init.Self;
		var renderSprites = self.Trait<RenderSprites>();
		animNotifications = self.TraitsImplementing<INotifyInfantryBodyAnimating>();
		move = self.Trait<IMove>();

		DefaultAnimation = new Animation(init.World, renderSprites.GetImage(self), RenderSprites.MakeFacingFunc(self));
		renderSprites.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled), info.Palette, info.IsPlayerPalette);
		PlayStandAnimation(self);
	}

	protected override void Created(Actor self)
	{
		rsm = self.TraitOrDefault<IRenderInfantrySequenceModifier>();
		var info = GetDisplayInfo();
		idleDelay = self.World.SharedRandom.Next(info.MinIdleDelay, info.MaxIdleDelay);
		
		base.Created(self);
	}

	protected virtual WithInfantryBodyInfo GetDisplayInfo()
	{
		return Info;
	}

	protected virtual string NormalizeInfantrySequence(Actor self, string baseSequence)
	{
		var prefix = IsModifyingSequence ? rsm.SequencePrefix : "";
		return DefaultAnimation.HasSequence(prefix + baseSequence) ? prefix + baseSequence : baseSequence;
	}

	void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament armament, Barrel barrel)
	{
		// Ensure this runs after Tick() to prevent overriding the animation
		self.World.AddFrameEndTask(_ => PlayAttackAnimation(self, armament, barrel));
	}

	void INotifyAttack.Attacking(Actor self, in Target target, Armament armament, Barrel barrel) { }

	protected virtual void PlayAttackAnimation(Actor self, Armament armament, Barrel barrel)
	{
		var info = GetDisplayInfo();
		var sequence = info.DefaultAttackSequence;

		if (info.AttackSequences.TryGetValue(armament.Info.Name, out var sequences) && sequences.Length > 0)
		{
			sequence = sequences[0];

			if (barrel != null && sequences.Length > 1)
				for (var i = 0; i < sequences.Length; i++)
					if (armament.Barrels[i] == barrel)
						sequence = sequences[i];
		}

		if (string.IsNullOrEmpty(sequence) || !DefaultAnimation.HasSequence(NormalizeInfantrySequence(self, sequence)))
			return;

		previousState = state;
		state = AnimationState.Attacking;
		DefaultAnimation.PlayThen(NormalizeInfantrySequence(self, sequence), () => PlayStandAnimation(self));
	}

	void ITick.Tick(Actor self)
	{
		foreach (var notif in animNotifications)
		{
			if (previousState != AnimationState.Moving && state == AnimationState.Moving)
			{
				notif.OnStartedRunningAnimation(self);
			}
			if (previousState == AnimationState.Moving && state != AnimationState.Moving)
			{
				notif.OnStoppedRunningAnimation(self);
			}
		}
		
		UpdateAnimationState(self);
	}

	protected virtual void UpdateAnimationState(Actor self)
	{
		UpdateSequenceModifierState();

		if (ShouldPlayMoveAnimation(self))
		{
			PlayMoveAnimation(self);
			dirty = false;
			return;
		}

		if (ShouldPlayStandAnimation(self))
		{
			PlayStandAnimation(self);
		}

		dirty = false;
	}

	protected virtual void UpdateSequenceModifierState()
	{
		if (rsm == null || wasModifying == rsm.IsModifyingSequence)
			return;

		dirty = true;
		wasModifying = rsm.IsModifyingSequence;
	}

	protected virtual bool ShouldPlayMoveAnimation(Actor self)
	{
		return (state != AnimationState.Moving || dirty) &&
			move.CurrentMovementTypes.HasMovementType(MovementType.Horizontal);
	}

	public virtual void PlayMoveAnimation(Actor self)
	{
		previousState = state;
		state = AnimationState.Moving;
		DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, GetDisplayInfo().MoveSequence));
		foreach(var notif in animNotifications)
		{
			notif.OnRunningAnimation(self);
		}
	}

	protected virtual bool ShouldPlayStandAnimation(Actor self)
	{
		return ((state == AnimationState.Moving || dirty) && !move.CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
			|| ((state == AnimationState.Idle || state == AnimationState.IdleAnimating) && !self.IsIdle);
	}

	public virtual void PlayStandAnimation(Actor self)
	{
		state = AnimationState.Waiting;

		var sequence = DefaultAnimation.GetRandomExistingSequence(Info.StandSequences, Game.CosmeticRandom);
		if (sequence == null)
			return;

		var normalized = NormalizeInfantrySequence(self, sequence);
		DefaultAnimation.PlayRepeating(normalized);
		foreach (var notif in animNotifications)
		{
			notif.OnStandingAnimation(self);
		}
	}

	void INotifyIdle.TickIdle(Actor self)
	{
		if (!AllowIdleAnimation(self))
			return;

		UpdateIdleAnimation(self);
	}

	protected virtual void UpdateIdleAnimation(Actor self)
	{
		if (state == AnimationState.Waiting)
		{
			PrepareIdleAnimation(self);
			return;
		}

		if (state == AnimationState.Idle && idleDelay > 0 && --idleDelay == 0)
		{
			PlayIdleAnimation(self);
		}
	}

	protected virtual bool AllowIdleAnimation(Actor self)
	{
		return GetDisplayInfo().IdleSequences.Length > 0 && !IsModifyingSequence;
	}

	protected virtual void PrepareIdleAnimation(Actor self)
	{
		previousState = state;
		state = AnimationState.Idle;
		var info = GetDisplayInfo();
		idleSequence = info.IdleSequences.Random(self.World.SharedRandom);
		idleDelay = self.World.SharedRandom.Next(info.MinIdleDelay, info.MaxIdleDelay);
	}

	protected virtual void PlayIdleAnimation(Actor self)
	{
		previousState = state;
		state = AnimationState.IdleAnimating;
		DefaultAnimation.PlayThen(idleSequence, () => PlayStandAnimation(self));
		foreach (var notif in animNotifications)
		{
			notif.OnIdleAnimation(self, idleSequence);
		}
	}

	protected enum AnimationState
	{
		Idle,
		Attacking,
		Moving,
		Waiting,
		IdleAnimating
	}
}
