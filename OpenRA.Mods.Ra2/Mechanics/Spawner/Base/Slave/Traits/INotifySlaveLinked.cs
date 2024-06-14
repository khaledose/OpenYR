using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;

[RequireExplicitImplementation]
public interface INotifySlaveLinked
{
	void Link(Actor self, Actor master);
}
