using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Interfaces;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Traits;

[Desc("This actor can spawn actors.")]
public class SpawnerMasterInfo : PausableConditionalTraitInfo
{
	[FieldLoader.Require]
	[Desc("Actors to spawn.")]
	public readonly string[] Actors = Array.Empty<string>();

	[Desc("Place slave actor will be spawned at.")]
	public readonly WVec[] SpawnOffsets = Array.Empty<WVec>();

	[Desc("Link spawned actor to parent.")]
	public readonly bool LinkToParent = true;

	[Desc("Allow slaves to respawn.")]
	public readonly bool AllowRespawn = true;

	[Desc("Respawn all dead slaves at once or one by one.")]
	public readonly bool RespawnAll = false;

	[Desc("Delay between each respawn.")]
	public readonly int RespawnDelay = 150;

	public override object Create(ActorInitializer init) { return new SpawnerMaster(this); }
}

public class SpawnerMaster : PausableConditionalTrait<SpawnerMasterInfo>, ITick, INotifyActorDisposing, INotifyOwnerChanged, INotifySlaveChanged
{
	protected readonly List<LinkedSlave> LinkedSlaves = new();
	[Sync]
	int respawnTicks;
	bool initialSpawn = true;

	public SpawnerMaster(SpawnerMasterInfo info) : base(info)
	{
		respawnTicks = Info.RespawnDelay;
	}

	protected override void Created(Actor self)
	{
		base.Created(self);
		InitializeSlaves(self);
		RefreshSlaves(self);
	}

	protected virtual void InitializeSlaves(Actor self)
	{
		for (var i = 0; i < Info.Actors.Length; i++)
		{
			LinkedSlaves.Add(new LinkedSlave
			{
				Name = Info.Actors[i],
				Offset = i < Info.SpawnOffsets.Length ? Info.SpawnOffsets[i] : WVec.Zero,
			});
		}
	}

	protected virtual Actor CreateSlave(Actor self)
	{
		var typeDictionary = new TypeDictionary { new OwnerInit(self.Owner) };
		if (Info.LinkToParent)
		{
			typeDictionary.Add(new ParentActorInit(self));
		}

		var linkedSlave = LinkedSlaves.FirstOrDefault(s => !s.IsAlive && !s.IsReady);
		if(linkedSlave is null)
		{
			return null;
		}

		linkedSlave.Actor = self.World.CreateActor(false, linkedSlave.Name, typeDictionary);
		var slaveLinked = linkedSlave.Actor
			.TraitsImplementing<ILinkSlaveWithMaster>()
			.FirstOrDefault();

		slaveLinked.Link(linkedSlave.Actor, self);
		return linkedSlave.Actor;
	}

	protected virtual void RefreshSlaves(Actor self)
	{
		if (!Info.AllowRespawn) return;

		if (!Info.RespawnAll && respawnTicks > 0 && !initialSpawn)
		{
			respawnTicks--;
			return;
		}

		var deadSlaves = LinkedSlaves.Where(s => !s.IsAlive);
		while (LinkedSlaves.Any(s => !s.IsAlive && !s.IsReady))
		{
			CreateSlave(self);

			if (!Info.RespawnAll && !initialSpawn)
			{
				respawnTicks = Info.RespawnDelay;
				return;
			}
		}

		if (initialSpawn)
		{
			initialSpawn = false;
		}
	}

	protected virtual void SpawnSlave(Actor self, Actor slave, WPos? customPosition = null)
	{
		var exit = self.RandomExitOrDefault(self.World, null);
		var centerPosition = customPosition ?? self.CenterPosition;

		self.World.AddFrameEndTask(w =>
		{
			if (self.IsDead)
				return;

			var spawnOffset = exit == null ? WVec.Zero : exit.Info.SpawnOffset;
			var positionable = slave.Trait<IPositionable>();
			positionable.SetPosition(slave, centerPosition + spawnOffset.Rotate(self.Orientation));
			positionable.SetCenterPosition(slave, centerPosition + spawnOffset.Rotate(self.Orientation));

			var location = self.World.Map.CellContaining(centerPosition + spawnOffset.Rotate(self.Orientation));

			var mv = slave.Trait<IMove>();
			slave.QueueActivity(mv.ReturnToCell(slave));

			slave.QueueActivity(mv.MoveTo(location, 2));

			w.Add(slave);
		});
	}

	protected virtual void Tick(Actor self)
	{
		if (IsTraitPaused || IsTraitDisabled)
		{
			return;
		}

		RefreshSlaves(self);
	}

	void ITick.Tick(Actor self)
	{
		Tick(self);
	}

	void INotifyActorDisposing.Disposing(Actor self)
	{
		var aliveSlaves = LinkedSlaves.Where(s => s.IsAlive);
		foreach (var slave in aliveSlaves)
		{
			var masterChanged = slave.Actor.TraitsImplementing<INotifyMasterChanged>().FirstOrDefault();
			masterChanged?.OnMasterKilled(slave.Actor);
		}
	}

	void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
	{
		var aliveSlaves = LinkedSlaves.Where(s => s.IsAlive);
		foreach (var slave in aliveSlaves)
		{
			var masterChanged = slave.Actor.TraitsImplementing<INotifyMasterChanged>().FirstOrDefault();
			masterChanged?.OnMasterOwnerChanged(slave.Actor);
		}
	}

	void INotifySlaveChanged.OnSlaveKilled(Actor self){}

	void INotifySlaveChanged.OnSlaveOwnerChanged(Actor self){}
}
