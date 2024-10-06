using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;

[RequireExplicitImplementation]
public interface ISoundPlayer
{
	string SoundsSet { get; }
	bool PlaySound(Actor self, string sound, string variant);
	bool HasSound(Actor self, string sound);
	string[] GetSounds(Actor self, string soundKey);
}
