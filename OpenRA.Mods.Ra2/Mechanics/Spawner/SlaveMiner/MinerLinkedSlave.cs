using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master;
using OpenRA.Mods.RA2.Mechanics.Spawner.SlaveMiner.Interfaces;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.SlaveMiner;
public class MinerLinkedSlave : LinkedSlave
{
	public INotifySlaveMinerTransformed MasterTransformed { get; set; }
}
