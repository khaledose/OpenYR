using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;

namespace OpenRA.Mods.Ra2.Mechanics.SoundPlayer.Traits;

[Desc("This actor has a voice.")]
public class SoundPlayerInfo : ConditionalTraitInfo
{
	[Desc("Which voice set to use. if null will default to actor name.")]
	public readonly string SoundsSet = null;

	public string GetSoundsSet(ActorInfo actor) => (SoundsSet ?? actor.Name).ToLowerInvariant();

	public override object Create(ActorInitializer init) { return new SoundPlayer(init, this); }
}

class SoundPlayer : ConditionalTrait<SoundPlayerInfo>, ISoundPlayer
{
	readonly string soundType;
	public SoundPlayer(ActorInitializer init, SoundPlayerInfo info) : base(info)
	{
		soundType = info.GetSoundsSet(init.Self.Info);
	}

	public string SoundsSet => Info.SoundsSet;

	public string[] GetSounds(Actor self, string sound)
	{
		var sounds = Array.Empty<string>();
		if (IsTraitDisabled)
			return sounds;

		if (string.IsNullOrEmpty(soundType))
			return sounds;

		if (!self.World.Map.Rules.Notifications.ContainsKey(soundType))
			return sounds;

		var hasSet = self.World.Map.Rules.Notifications.TryGetValue(soundType, out var soundsSet);
		if (!hasSet || soundsSet is null)
			return sounds;

		soundsSet.Notifications.TryGetValue(sound, out sounds);
		return sounds;
	}

	public bool HasSound(Actor self, string sound)
	{
		if (IsTraitDisabled)
			return false;

		if (string.IsNullOrEmpty(soundType))
			return false;

		if (!self.World.Map.Rules.Notifications.ContainsKey(soundType))
			return false;

		var hasSet = self.World.Map.Rules.Notifications.TryGetValue(soundType, out var sounds);
		return hasSet && sounds != null && sounds.Notifications.ContainsKey(sound);
	}

	public bool PlaySound(Actor self, string sound, string variant)
	{
		if (!HasSound(self, sound))
			return false;

		return Game.Sound.PlayPredefined(SoundType.World, self.World.Map.Rules, null, null, soundType, sound, variant, false, self.CenterPosition, 1f, false);
	}
}
