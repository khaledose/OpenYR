using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Deploy.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Deploy.Activities;

public class Deploy : Activity
{
	readonly Deployable deployable;
	public Deploy(Actor self, Deployable deploy)
	{
		deployable = deploy;
		IsInterruptible = false;
	}

	public override bool Tick(Actor self)
	{
		if (IsCanceling)
			return true;

		switch (deployable.CurrentState)
		{
			case DeployState.Undeployed:
				deployable.Deploy(self);
				break;
			case DeployState.Deployed:
				deployable.Undeploy(self);
				break;
			case DeployState.Deploying:
			case DeployState.Undeploying:
			default:
				return true;
		}

		return true;
	}
}
