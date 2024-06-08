using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master;

public class LinkedSpawnerSlave
{
	public string Name { get; set; }
	public Actor Actor { get; set; }
	public SpawnerSlave SpawnerSlave { get; set; }
	public INotifyMasterChanged MasterChanged { get; set; }
	public WVec Offset { get; set; }
	public bool IsReady => Actor is not null && !Actor.IsDead && !Actor.IsInWorld;
	public bool IsAlive => Actor?.IsInWorld ?? false;
}
