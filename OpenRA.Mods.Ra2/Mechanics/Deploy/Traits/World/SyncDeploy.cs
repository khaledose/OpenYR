using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using WorldActor = OpenRA.World;

namespace OpenRA.Mods.Ra2.Mechanics.Deploy.Traits.World;

[TraitLocation(SystemActors.World)]
public class SyncDeployInfo : TraitInfo
{
	[Desc("The condition to grant while the actor is deploying.")]
	public readonly DeployState SyncOnState = DeployState.Deployed;

	public override object Create(ActorInitializer init)
	{
		return new SyncDeploy(init, this);
	}
}

public class SyncDeploy : ITick, INotifySelection
{
	readonly SyncDeployInfo info;
	readonly WorldActor world;
	IEnumerable<Deployable> deployables;
	bool NeedToSync 
	{
		get
		{
			if (deployables is null)
				return false;

			var hasDeployed = false;
			var hasUndeployed = false;

			foreach (var deployable in deployables)
			{
				if (deployable.CurrentState == DeployState.Deployed)
					hasDeployed = true;
				else if (deployable.CurrentState == DeployState.Undeployed)
					hasUndeployed = true;

				if (hasDeployed && hasUndeployed)
					return true;
			}

			return false;
		}
	}

	public SyncDeploy(ActorInitializer init, SyncDeployInfo info)
	{
		world = init.World;
		this.info = info;
	}

	void Sync()
	{
		if (deployables is null || !NeedToSync)
			return;

		foreach (var deployable in deployables)
		{
			if (deployable.CurrentState != info.SyncOnState)
				continue;

			deployable.NeedsSync = true;
		}
	}

	void INotifySelection.SelectionChanged()
	{
		deployables = world.Selection.Actors
			.Select(a => a.TraitOrDefault<Deployable>())
			.Where(d => d is not null);

		Sync();
	}

	void ITick.Tick(Actor self)
	{
		if (deployables is null || NeedToSync)
		{
			Sync();
			return;
		}

		foreach (var deployable in deployables)
		{
			deployable.NeedsSync = false;
		}
	}
}
