using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;

[RequireExplicitImplementation]
public interface INotifyDeployedInfantryBodyAnimating
{
	void OnDeployingAnimation(Actor self);
	void OnDeployedAnimation(Actor self);
	void OnUndeployingAnimation(Actor self);
}
