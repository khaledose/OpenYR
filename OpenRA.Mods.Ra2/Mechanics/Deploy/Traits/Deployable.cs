using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using DeployActivity = OpenRA.Mods.Ra2.Mechanics.Deploy.Activities.Deploy;

namespace OpenRA.Mods.Ra2.Mechanics.Deploy.Traits;

public class DeployableInfo : PausableConditionalTraitInfo
{
	[GrantedConditionReference]
	[Desc("The condition to grant while the actor is deploying.")]
	public readonly string DeployingCondition = null;

	[GrantedConditionReference]
	[Desc("The condition to grant while the actor is deployed.")]
	public readonly string DeployedCondition = null;

	[GrantedConditionReference]
	[Desc("The condition to grant while the actor is undeploying.")]
	public readonly string UndeployingCondition = null;

	[GrantedConditionReference]
	[Desc("The condition to grant while the actor is undeployed.")]
	public readonly string UndeployedCondition = null;

	[Desc("Sync the deploy with selection.")]
	public readonly bool SyncDeploy = true;

	[Desc("The terrain types that this actor can deploy on. Leave empty to allow any.")]
	public readonly HashSet<string> AllowedTerrainTypes = new();

	[Desc("Can this actor deploy on slopes?")]
	public readonly bool CanDeployOnRamps = false;

	[CursorReference]
	[Desc("Cursor to display when able to (un)deploy the actor.")]
	public readonly string DeployCursor = "deploy";

	[CursorReference]
	[Desc("Cursor to display when unable to (un)deploy the actor.")]
	public readonly string DeployBlockedCursor = "deploy-blocked";

	public override object Create(ActorInitializer init) { return new Deployable(init, this); }

}

public class Deployable : PausableConditionalTrait<DeployableInfo>,
	IResolveOrder, IIssueOrder, IIssueDeployOrder, INotifyDeployComplete
{
	IEnumerable<INotifyDeployTriggered> deployNotifications;
	public DeployState CurrentState {  get; private set; }
	public bool NeedsSync { get; set; } = false;

	readonly Actor self;
	int token = Actor.InvalidConditionToken;

	public Deployable(ActorInitializer init, DeployableInfo info)
		: base(info)
	{
		self = init.Self;
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		deployNotifications = self.TraitsImplementing<INotifyDeployTriggered>();
		OnUndeployCompleted(self);
	}

	IEnumerable<IOrderTargeter> IIssueOrder.Orders
	{
		get
		{
			if (!IsTraitDisabled)
				yield return new DeployOrderTargeter("Deploy", 5,
					() => CanDeploy(self) ? Info.DeployCursor : Info.DeployBlockedCursor);
		}
	}

	bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

	Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
	{
		return new Order("Deploy", self, queued);
	}

	Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
	{
		if (order.OrderID == "Deploy")
			return new Order(order.OrderID, self, target, queued);

		return null;
	}

	void IResolveOrder.ResolveOrder(Actor self, Order order)
	{
		if (IsTraitDisabled || IsTraitPaused)
			return;

		if (order.OrderString != "Deploy")
			return;

		if (Info.SyncDeploy && NeedsSync)
			return;

		self.QueueActivity(order.Queued, new DeployActivity(self, this));
	}

	void INotifyDeployComplete.FinishedDeploy(Actor self)
	{
		OnDeployCompleted(self);
	}

	void INotifyDeployComplete.FinishedUndeploy(Actor self)
	{
		OnUndeployCompleted(self);
	}

	public virtual bool CanDeploy(Actor self)
	{
		if (IsTraitPaused || IsTraitDisabled)
			return false;

		return IsValidTerrain(self, self.Location) || (CurrentState == DeployState.Deployed);
	}

	bool IsValidTerrain(Actor self, CPos location)
	{
		return IsValidTerrainType(self, location) && IsValidRampType(self, location);
	}

	bool IsValidTerrainType(Actor self, CPos location)
	{
		if (!self.World.Map.Contains(location))
			return false;

		if (Info.AllowedTerrainTypes.Count == 0)
			return true;

		var terrainType = self.World.Map.GetTerrainInfo(location).Type;

		return Info.AllowedTerrainTypes.Contains(terrainType);
	}

	bool IsValidRampType(Actor self, CPos location)
	{
		if (Info.CanDeployOnRamps)
			return true;

		var map = self.World.Map;
		return !map.Ramp.Contains(location) || map.Ramp[location] == 0;
	}

	void GrantCondition(Actor self, string condition)
	{
		if (token != Actor.InvalidConditionToken)
			token = self.RevokeCondition(token);

		if (condition is null)
			return;

		token = self.GrantCondition(condition);
	}

	public virtual void Deploy(Actor self)
	{
		CurrentState = DeployState.Deploying;
		GrantCondition(self, Info.DeployingCondition);

		foreach (var notify in deployNotifications)
		{
			notify.Deploy(self, false);
		}
	}

	public virtual void OnDeployCompleted(Actor self)
	{
		CurrentState = DeployState.Deployed;
		GrantCondition(self, Info.DeployedCondition);
	}

	public virtual void Undeploy(Actor self)
	{
		CurrentState = DeployState.Undeploying;
		GrantCondition(self, Info.UndeployingCondition);

		foreach (var notify in deployNotifications)
		{
			notify.Undeploy(self, false);
		}
	}

	public virtual void OnUndeployCompleted(Actor self)
	{
		CurrentState = DeployState.Undeployed;
		GrantCondition(self, Info.UndeployedCondition);
	}
}
