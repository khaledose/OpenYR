using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Interfaces;

[RequireExplicitImplementation]
public interface INotifySlaveEntering
{
	void OnSlaveEntering(Actor self, Actor slave);
	void OnSlaveEntered(Actor self, Actor slave);
}
