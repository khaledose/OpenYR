namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Interfaces;

public interface INotifySlaveChanged
{
	void OnSlaveKilled(Actor self);

	void OnSlaveOwnerChanged(Actor self);
}
