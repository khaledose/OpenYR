using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Slave.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Spawner.Base.Master.Traits;

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

public class SpawnerMaster : PausableConditionalTrait<SpawnerMasterInfo>, ITick, INotifyKilled, INotifyOwnerChanged, INotifySlaveChanged
{
	protected readonly List<LinkedSlave> LinkedSlaves = new();
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

	protected virtual void CreateSlave(Actor self, LinkedSlave linkedSlave)
	{
		var typeDictionary = new TypeDictionary { new OwnerInit(self.Owner) };
		if (Info.LinkToParent)
		{
			typeDictionary.Add(new ParentActorInit(self));
		}

		linkedSlave.Actor = self.World.CreateActor(false, linkedSlave.Name, typeDictionary);
		linkedSlave.MasterChanged = linkedSlave.Actor
			 .TraitsImplementing<INotifyMasterChanged>()
			 .FirstOrDefault();
		linkedSlave.SlaveLinked = linkedSlave.Actor
			 .TraitsImplementing<INotifySlaveLinked>()
			 .FirstOrDefault();
		linkedSlave.SlaveLinked.Link(linkedSlave.Actor, self);
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
		foreach (var slave in deadSlaves)
		{
			CreateSlave(self, slave);

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

	protected void Tick(Actor self)
	{
		RefreshSlaves(self);
	}

	void ITick.Tick(Actor self)
	{
		Tick(self);

		foreach (var slave in LinkedSlaves.Where(s => s.IsReady))
			SpawnSlave(self, slave.Actor);
	}

	void INotifyKilled.Killed(Actor self, AttackInfo e)
	{
		var aliveSlaves = LinkedSlaves.Where(s => s.IsAlive);
		foreach (var slave in aliveSlaves)
		{
			slave.MasterChanged?.OnMasterKilled(slave.Actor);
		}
	}

	void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
	{
		var aliveSlaves = LinkedSlaves.Where(s => s.IsAlive);
		foreach (var slave in aliveSlaves)
		{
			slave.MasterChanged?.OnMasterOwnerChanged(slave.Actor);
		}
	}

	void INotifySlaveChanged.OnSlaveKilled(Actor self){}

	void INotifySlaveChanged.OnSlaveOwnerChanged(Actor self){}
}
