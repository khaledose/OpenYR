using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;

namespace OpenRA.Mods.Ra2.Mechanics.Extensions;

public static class ActorExtension
{
	public static void PlaySound(this Actor self, string sound)
	{
		if (sound == null)
			return;

		foreach (var soundPlayer in self.TraitsImplementing<ISoundPlayer>())
		{
			soundPlayer.PlaySound(self, sound, self.Owner.Faction.InternalName);
		}
	}
}
