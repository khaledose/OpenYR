using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Interfaces;

[RequireExplicitImplementation]
public interface INotifyMasterChanged
{
	void OnMasterKilled(Actor self);

	void OnMasterOwnerChanged(Actor self);
}
