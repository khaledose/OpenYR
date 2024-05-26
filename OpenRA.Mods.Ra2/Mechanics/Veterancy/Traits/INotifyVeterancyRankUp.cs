using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits;

[RequireExplicitImplementation]
public interface INotifyVeterancyRankUp
{
	void OnRankUp(Actor self);
}
