using OpenRA.Mods.Ra2.Mechanics.Extensions;
using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;
using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.World;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.Sound;

public class WithInfantryBodySoundInfo : TraitInfo
{
	[Desc("Sounds to play while idle animating.")]
	public readonly Dictionary<string, string> IdleSounds = new();

	[Desc("Sound to play when start moving.")]
	public readonly string RunStartSound = null;

	[Desc("Sound to play looped while moving.")]
	public readonly string RunLoopSound = null;

	[Desc("Sound to play when stop moving.")]
	public readonly string RunStopSound = null;

	[Desc("Sound to play while standing.")]
	public readonly string StandSound = null;

	public override object Create(ActorInitializer init)
	{
		return new WithInfantryBodySound(this);
	}
}

public class WithInfantryBodySound : INotifyInfantryBodyAnimating, INotifyCreated
{
	public readonly WithInfantryBodySoundInfo Info;
	WithWorldMoveSound worldTrait;

	public WithInfantryBodySound(WithInfantryBodySoundInfo info)
	{
		Info = info;
	}

	void INotifyCreated.Created(Actor self)
	{
		worldTrait = self.World.WorldActor.TraitOrDefault<WithWorldMoveSound>();
	}

	public void OnIdleAnimation(Actor self, string sequence)
	{
		var sequenceFound = Info.IdleSounds.TryGetValue(sequence, out var sound);
		if (sequenceFound && !string.IsNullOrEmpty(sound))
		{
			self.PlaySound(sound);
		}
	}

	public void OnRunningAnimation(Actor self)
	{
		worldTrait.Enable(self, Info.RunLoopSound);
	}

	public void OnStandingAnimation(Actor self)
	{
		if (!string.IsNullOrEmpty(Info.StandSound))
		{
			self.PlaySound(Info.StandSound);
		}
	}

	public void OnStartedRunningAnimation(Actor self)
	{
		if (!string.IsNullOrEmpty(Info.RunStartSound))
		{
			self.PlaySound(Info.RunStartSound);
		}

		OnRunningAnimation(self);
	}

	public void OnStoppedRunningAnimation(Actor self)
	{
		worldTrait.Disable(self, Info.RunLoopSound);
		if (!string.IsNullOrEmpty(Info.RunStopSound))
		{
			self.PlaySound(Info.RunStopSound);
		}
	}
}
