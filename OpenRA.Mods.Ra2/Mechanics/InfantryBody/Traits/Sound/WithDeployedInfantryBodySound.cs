using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Ra2.Mechanics.Extensions;
using OpenRA.Mods.Ra2.Mechanics.InfantryBody.Interfaces;
using OpenRA.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.InfantryBody.Traits.Sound;

public class WithDeployedInfantryBodySoundInfo : TraitInfo
{
	[Desc("Sound to play while deploying.")]
	public readonly string DeployingSound = null;

	[Desc("Sound to play while deployed.")]
	public readonly string DeployedSound = null;

	[Desc("Sound to play while undeploying.")]
	public readonly string UndeployingSound = null;

	public override object Create(ActorInitializer init)
	{
		return new WithDeployedInfantryBodySound(this);
	}
}

public class WithDeployedInfantryBodySound : INotifyDeployedInfantryBodyAnimating
{
	public readonly WithDeployedInfantryBodySoundInfo Info;
	public WithDeployedInfantryBodySound(WithDeployedInfantryBodySoundInfo info)
	{
		Info = info;
	}

	public void OnDeployedAnimation(Actor self)
	{
		if (!string.IsNullOrEmpty(Info.DeployedSound))
		{
			self.PlaySound(Info.DeployedSound);
		}
	}

	public void OnDeployingAnimation(Actor self)
	{
		if (!string.IsNullOrEmpty(Info.DeployingSound))
		{
			self.PlaySound(Info.DeployingSound);
		}
	}

	public void OnUndeployingAnimation(Actor self)
	{
		if (!string.IsNullOrEmpty(Info.UndeployingSound))
		{
			self.PlaySound(Info.UndeployingSound);
		}
	}
}
