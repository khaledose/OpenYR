namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;

[RequireExplicitImplementation]
public interface ILinkSpawners
{
	void Link(Actor self, Actor master, MasterSpawner spawner);
}
