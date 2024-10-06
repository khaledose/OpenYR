using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;

[RequireExplicitImplementation]
public interface INotifyInfantryBodyAnimating
{
	void OnIdleAnimation(Actor self, string sequence);
	void OnStartedRunningAnimation(Actor self);
	void OnRunningAnimation(Actor self);
	void OnStoppedRunningAnimation(Actor self);
	void OnStandingAnimation(Actor self);
}
