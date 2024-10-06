using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.World;
[TraitLocation(SystemActors.World)]
[Desc("Prevents WithMoveSound to play multiple times.")]
public class WithWorldMoveSoundInfo : TraitInfo
{
	public override object Create(ActorInitializer init)
	{
		return new WithWorldMoveSound();
	}
}

public class WithWorldMoveSound : ITick, IWorldLoaded
{
	record Entry(ISound Sound, List<Actor> Actors);

	readonly Dictionary<string, Entry> playing = new ();
	WorldRenderer worldRenderer;

	void IWorldLoaded.WorldLoaded(OpenRA.World world, WorldRenderer worldRenderer)
	{
		this.worldRenderer = worldRenderer;
	}

	public void Enable(Actor actor, string soundName)
	{
		if (!playing.TryGetValue(soundName, out var entry))
		{
			var sound = Game.Sound.PlayLooped(SoundType.World, soundName, actor.CenterPosition);

			if (sound == null)
				return;

			playing.Add(soundName, entry = new Entry(sound, new List<Actor> { actor }));
		}

		if (!entry.Actors.Contains(actor))
			entry.Actors.Add(actor);

		Game.Sound.SetLooped(entry.Sound, true);
	}

	public void Disable(Actor actor, string sound)
	{
		if (!playing.TryGetValue(sound, out var entry))
			return;

		entry.Actors.Remove(actor);

		if (entry.Actors.Count > 0)
			Game.Sound.SetLooped(entry.Sound, false);
	}

	void ITick.Tick(Actor self)
	{
		foreach (var sound in playing.Keys.ToArray())
		{
			var entry = playing[sound];

			if (entry.Actors.Count > 0)
			{
				if (entry.Sound.Complete)
					playing.Remove(sound);
			}
			else if (worldRenderer != null)
			{
				Game.Sound.SetPosition(
					entry.Sound,
					entry.Actors.MinBy(actor => (actor.CenterPosition - worldRenderer.Viewport.CenterPosition).Length).CenterPosition
				);
			}
		}
	}
}
