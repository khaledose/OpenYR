using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Sequence.Traits;

public class SequenceNormalizerInfo : TraitInfo
{
	public readonly IReadOnlyDictionary<DamageState, string> DamagePrefixes = new Dictionary<DamageState, string>()
	{
		[DamageState.Light] = "scuffed-",
		[DamageState.Medium] = "scratched-",
		[DamageState.Heavy] = "damaged-",
		[DamageState.Critical] = "critical-",
		[DamageState.Dead] = "dead-",
	};

	public override object Create(ActorInitializer init)
	{
		return new SequenceNormalizer(init, this);
	}
}

public class SequenceNormalizer
{
	public readonly SequenceNormalizerInfo Info;
	readonly Actor self;

	public SequenceNormalizer(ActorInitializer init, SequenceNormalizerInfo sequenceNormalizerInfo)
	{
		Info = sequenceNormalizerInfo;
		self = init.Self;
	}

	public string NormalizeSequence(Animation anim, string sequence, DamageState damageState)
	{
		if (Info.DamagePrefixes.TryGetValue(damageState, out var prefix) && !string.IsNullOrEmpty(prefix))
		{
			var normalizedSequence = prefix + UnnormalizeSequence(sequence);
			return anim.HasSequence(normalizedSequence) ? normalizedSequence : anim.CurrentSequence.Name;
		}

		return anim.CurrentSequence.Name;
	}

	public string UnnormalizeSequence(string sequence)
	{
		var unnormalizedSequence = Info.DamagePrefixes
			.FirstOrDefault(s => sequence.StartsWith(s.Value, StringComparison.Ordinal))
			.Value;

		return unnormalizedSequence != null ? sequence[unnormalizedSequence.Length..] : sequence;
	}
}

