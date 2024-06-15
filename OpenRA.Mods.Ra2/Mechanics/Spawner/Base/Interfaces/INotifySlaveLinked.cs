using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Interfaces;

[RequireExplicitImplementation]
public interface INotifySlaveLinked
{
	void Link(Actor self, Actor master);
}
