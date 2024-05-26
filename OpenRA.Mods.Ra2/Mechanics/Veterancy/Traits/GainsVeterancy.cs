using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Veterancy.Traits;

[Desc("This actor's experience increases when it has killed a GivesExperience actor.")]
public class GainsVeterancyInfo : TraitInfo
{
	[FieldLoader.Require]
	[Desc("XP requirements for each level as a percentage of our own value.")]
	public readonly int[] ExperiencePerLevel = Array.Empty<int>();

	[Desc("Image for the level up sprite.")]
	public readonly string LevelUpImage = null;

	[SequenceReference(nameof(LevelUpImage), allowNullImage: true)]
	[Desc("Sequence for the level up sprite. Needs to be present on LevelUpImage.")]
	public readonly string LevelUpSequence = "levelup";

	[PaletteReference]
	[Desc("Palette for the level up sprite.")]
	public readonly string LevelUpPalette = "effect";

	[Desc("Multiplier to apply to the Conditions keys. Defaults to the actor's value.")]
	public readonly int ExperienceModifier = -1;

	[Desc("Should the level-up animation be suppressed when actor is created?")]
	public readonly bool SuppressLevelupAnimation = true;

	[NotificationReference("Sounds")]
	public readonly string LevelUpNotification = null;

	[TranslationReference(optional: true)]
	public readonly string LevelUpTextNotification = null;

	public override object Create(ActorInitializer init) { return new GainsVeterancy(init, this); }
}

public class GainsVeterancy : INotifyCreated, ISync, IResolveOrder, ITransformActorInitModifier
{
	readonly Actor self;
	readonly GainsVeterancyInfo info;
	readonly int initialExperience;

	readonly List<int> nextLevel = new();

	// Stored as a percentage of our value
	[Sync]
	public int Experience { get; private set; }

	[Sync]
	public int Level { get; private set; }
	public readonly int MaxLevel;

	public GainsVeterancy(ActorInitializer init, GainsVeterancyInfo info)
	{
		self = init.Self;
		this.info = info;
		Experience = 0;
		MaxLevel = info.ExperiencePerLevel.Length;
		initialExperience = init.GetValue<ExperienceInit, int>(info, 0);
	}

	void INotifyCreated.Created(Actor self)
	{
		var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
		var requiredExperience = info.ExperienceModifier < 0 ? (valued != null ? valued.Cost : 1) : info.ExperienceModifier;
		foreach (var xp in info.ExperiencePerLevel)
		{
			nextLevel.Add(Util.ApplyPercentageModifiers(requiredExperience, new[] { xp }));
		}

		if (initialExperience > 0)
			GiveExperience(initialExperience, info.SuppressLevelupAnimation);
	}

	public bool CanGainLevel => Level < MaxLevel;

	public void GiveLevels(int numLevels, bool silent = false)
	{
		if (MaxLevel == 0)
			return;

		var newLevel = Math.Min(Level + numLevels, MaxLevel);
		GiveExperience(nextLevel[newLevel - 1] - Experience, silent);
	}

	public void GiveExperience(int amount, bool silent = false)
	{
		if (amount < 0)
			throw new ArgumentException("Revoking experience is not implemented.", nameof(amount));

		if (MaxLevel == 0)
			return;

		Experience = (Experience + amount).Clamp(0, nextLevel[MaxLevel - 1]);

		while (Level < MaxLevel && Experience >= nextLevel[Level])
		{
			Level++;
			foreach (var notify in self.TraitsImplementing<INotifyVeterancyRankUp>())
			{
				if (notify.IsTraitEnabled())
				{
					notify.OnRankUp(self);
				}
			}

			if (!silent)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", info.LevelUpNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(self.Owner, info.LevelUpTextNotification);

				if (info.LevelUpImage != null && info.LevelUpSequence != null)
					self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self, w, info.LevelUpImage, info.LevelUpSequence, info.LevelUpPalette)));
			}
		}
	}

	public void ResolveOrder(Actor self, Order order)
	{
		if (order.OrderString == "DevLevelUp")
		{
			var developerMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
			if (!developerMode.Enabled)
				return;

			if ((int)order.ExtraData > 0)
				GiveLevels((int)order.ExtraData);
			else
				GiveLevels(1);
		}
	}

	void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)
	{
		init.Add(new ExperienceInit(info, Experience));
	}
}

sealed class ExperienceInit : ValueActorInit<int>
{
	public ExperienceInit(TraitInfo info, int value)
		: base(info, value) { }
}
