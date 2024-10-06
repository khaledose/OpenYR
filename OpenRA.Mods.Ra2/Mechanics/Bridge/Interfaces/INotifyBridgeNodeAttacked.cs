using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Bridge.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Bridge.Interfaces;

public interface INotifyBridgeNodeAttacked
{
	void OnPrevNodeDamaged(IBridgeNode node);
	void OnNextNodeDamaged(IBridgeNode node);
}

public interface IBridgeNode
{
	int BridgeId { get; set; }
	int Id { get; set; }
	BridgeDirection Direction { get; set; }
	Actor Actor { get; init; }
	BridgeNodeInfo Info { get; init; }
	BuildingInfo BuildingInfo { get; init; }
	IHealth Health { get; init; }
	IBridgeNode PrevNode { get; set; }
	IBridgeNode NextNode { get; set; }
	void Damage(Damage damage);
	void Repair();
}

public interface IBridgeHut
{
	int BridgeId { get; set; }
	BridgeDirection Direction { get; set; }
	Actor Actor { get; init; }
	Traits.BridgeHutInfo Info { get; init; }
	void Demolish(Actor self);
	void Repair(Actor self);
}
