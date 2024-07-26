using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Interfaces;

[RequireExplicitImplementation]
public interface INotifySlaveMinerTransformed
{
	public void OnTransformCompleted(Actor self, MasterMiner masterMiner);
}
