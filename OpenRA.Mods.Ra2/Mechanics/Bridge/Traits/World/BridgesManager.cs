using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Interfaces;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Bridge.Traits.World;

public record Bridge(int Id, List<IBridgeHut> Huts, List<IBridgeNode> Nodes);

[TraitLocation(SystemActors.World)]
public class BridgesManagerInfo : TraitInfo
{
	public readonly string TerrainType = "Bridge";
	public override object Create(ActorInitializer init) { return new BridgesManager(init.World, this); }
}

public class BridgesManager : IPostWorldLoaded
{
	public readonly BridgesManagerInfo Info;
	public readonly OpenRA.World World;
	readonly CellLayer<IBridgeNode> bridgeNodes;
	readonly List<Bridge> bridges = new();
	readonly List<IBridgeHut> allBridgeHuts = new();
	public IBridgeNode this[CPos cell] => bridgeNodes[cell];

	public BridgesManager(OpenRA.World world, BridgesManagerInfo info)
	{
		World = world;
		Info = info;
		bridgeNodes = new CellLayer<IBridgeNode>(world.Map);
	}

	public void AddNode(IBridgeNode bridgeNode)
	{
		var terrainIndex = World.Map.Rules.TerrainInfo.GetTerrainIndex(Info.TerrainType);
		foreach (var cell in bridgeNode.BuildingInfo.PathableTiles(bridgeNode.Actor.Location))
		{
			bridgeNodes[cell] = bridgeNode;
			World.Map.CustomTerrain[cell] = terrainIndex;
		}
	}

	public void RemoveNode(IBridgeNode bridgeNode)
	{
		World.AddFrameEndTask(w =>
		{
			if (bridgeNodes[bridgeNode.Actor.Location] is null)
				return;

			DestroyFloatingNodes(bridgeNode);
		});
	}

	public void OnNodeDamaged(IBridgeNode bridgeNode, Actor attacker)
	{
		World.AddFrameEndTask(w =>
		{
			var bridge = bridges.FirstOrDefault(b => b.Id == bridgeNode.BridgeId);
			if (bridge == null) return;

			SpreadDamage(bridge, bridgeNode);
			UpdateBridgeVisuals(bridge);
		});
	}

	void IPostWorldLoaded.PostWorldLoaded(OpenRA.World w, WorldRenderer wr)
	{
		// Retrieve all IBridgeHut actors once
		allBridgeHuts.AddRange(w.Actors
			.Select(a => a.TraitsImplementing<IBridgeHut>().FirstOrDefault())
			.Where(hut => hut != null));

		var bridgeHeadNodes = w.Actors
			.Select(a => a.TraitsImplementing<IBridgeNode>().FirstOrDefault())
			.Where(t => t != null && t.Info.Type == BridgeNodeType.Head && t.Id == -1);

		var currentBridgeId = 0;
		foreach (var startNode in bridgeHeadNodes)
		{
			var bridge = CreateBridge(startNode, currentBridgeId++);
			if (bridge != null)
				bridges.Add(bridge);
		}
	}

	Bridge CreateBridge(IBridgeNode startNode, int bridgeId)
	{
		var bridgeNodes = new List<IBridgeNode>();
		var currentNode = startNode;
		var currentNodeId = 0;
		var direction = FindNodeDirection(startNode);

		while (currentNode != null)
		{
			currentNode.BridgeId = bridgeId;
			currentNode.Id = currentNodeId++;
			currentNode.Direction = direction;
			bridgeNodes.Add(currentNode);
			if (currentNode.Info.Type == BridgeNodeType.Head && currentNode.Id > startNode.Id)
			{
				currentNode.Direction = direction == BridgeDirection.Positive ? BridgeDirection.Negative : BridgeDirection.Positive;
				break;
			}

			currentNode = GetNextNode(currentNode);
		}

		var bridgeHuts = GetBridgeHuts(bridgeId);

		return new Bridge(bridgeId, bridgeHuts, bridgeNodes);
	}

	List<IBridgeHut> GetBridgeHuts(int bridgeId)
	{
		// Collect bridge huts associated with this bridge by searching nearby nodes.
		var bridgeHuts = new List<IBridgeHut>();

		foreach (var hut in allBridgeHuts)
		{
			// Optimize tile searching by limiting the number of nodes checked in the circle
			var nearbyNodes = World.Map.FindTilesInCircle(hut.Actor.Location, 2);

			foreach (var tile in nearbyNodes)
			{
				if (bridgeNodes.TryGetValue(tile, out var node) && node?.BridgeId == bridgeId)
				{
					hut.BridgeId = bridgeId;
					hut.Direction = node.Direction;
					bridgeHuts.Add(hut);
					break; // Found a relevant hut, break to the next hut
				}
			}
		}

		return bridgeHuts;
	}

	IBridgeNode GetNextNode(IBridgeNode currentNode)
	{
		var offset = currentNode.Info.Offset * (int)currentNode.Direction;
		var nextNodeLocation = currentNode.Actor.Location + offset;
		bridgeNodes.TryGetValue(nextNodeLocation, out var nextNode);

		if (nextNode != null)
		{
			currentNode.NextNode = nextNode;
			nextNode.PrevNode = currentNode;
		}

		return nextNode;
	}

	BridgeDirection FindNodeDirection(IBridgeNode node)
	{
		if (bridgeNodes.TryGetValue(node.Actor.Location - node.Info.Offset, out var prevNode) && prevNode is not null)
		{
			return BridgeDirection.Negative;
		}

		return BridgeDirection.Positive;
	}

	void SpreadDamage(Bridge bridge, IBridgeNode startNode)
	{
		var initialDamage = startNode.Health.MaxHP - startNode.Health.HP;
		SpreadDamageInDirection(bridge, startNode, initialDamage, 1);  // Right direction
		SpreadDamageInDirection(bridge, startNode, initialDamage, -1); // Left direction
	}

	void SpreadDamageInDirection(Bridge bridge, IBridgeNode startNode, int initialDamage, int direction)
	{
		var filteredNodes = bridge.Nodes
			.Where(n => (direction > 0 ? n.Id > startNode.Id : n.Id < startNode.Id) && n.Info.Type == BridgeNodeType.Segment)
			.OrderBy(n => direction > 0 ? n.Id : -n.Id);

		if (!filteredNodes.Any())
			return;

		var maxDmgNode = filteredNodes
			.Where(n => n.Health.DamageState >= startNode.Health.DamageState)
			.Select(n => new { n.Id, Damage = n.Health.MaxHP - n.Health.HP })
			.FirstOrDefault();

		if (maxDmgNode is null || maxDmgNode.Damage == 0)
			return;

		foreach (var node in filteredNodes.Where(n => direction > 0 ? n.Id < maxDmgNode.Id : n.Id > maxDmgNode.Id))
		{
			node.Repair();
			node.Damage(new Damage(Math.Min(initialDamage, maxDmgNode.Damage)));
		}
	}

	void UpdateBridgeVisuals(Bridge bridge)
	{
		foreach (var node in bridge.Nodes)
		{
			var currentState = node.Actor.GetDamageState();
			var prevState = node.PrevNode?.Actor.GetDamageState() ?? DamageState.Undamaged;
			var nextState = node.NextNode?.Actor.GetDamageState() ?? DamageState.Undamaged;
			if (nextState <= currentState && prevState <= currentState)
				continue;

			var notifyAttacked = node.Actor.TraitsImplementing<INotifyBridgeNodeAttacked>().FirstOrDefault();
			if (nextState > currentState)
				notifyAttacked?.OnNextNodeDamaged(node.NextNode);

			if (prevState > currentState)
				notifyAttacked?.OnPrevNodeDamaged(node.PrevNode);
		}
	}

	void DestroyFloatingNodes(IBridgeNode bridgeNode)
	{
		var bridge = bridges.FirstOrDefault(b => b.Id == bridgeNode.BridgeId);
		if (bridge == null) return;

		var destroyFrom = bridge.Nodes
			.Where(n => n.Id != bridgeNode.Id && n.Actor.IsDead)
			.Select(n => n.Id)
			.DefaultIfEmpty(bridgeNode.Id)
			.First();
		var minIndex = Math.Min(destroyFrom, bridgeNode.Id);
		var maxIndex = Math.Max(destroyFrom, bridgeNode.Id);

		for (var i = minIndex; i <= maxIndex; i++)
		{
			DestroyNode(bridge.Nodes[i]);
		}
	}

	void DestroyNode(IBridgeNode bridgeNode)
	{
		foreach (var c in bridgeNode.BuildingInfo.PathableTiles(bridgeNode.Actor.Location))
		{
			if (bridgeNodes[c] == bridgeNode)
				bridgeNodes[c] = null;
			World.Map.CustomTerrain[c] = byte.MaxValue;

			foreach (var actor in World.ActorMap.GetActorsAt(c))
			{
				if (actor.Info.HasTraitInfo<IPositionableInfo>() && !actor.Trait<IPositionable>().CanExistInCell(c))
					actor.Kill(World.WorldActor);
			}
		}

		bridgeNode.Actor.Kill(World.WorldActor);
	}
}
