using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Ra2.Mechanics.Extensions;
public static class ResourceLayerExtension
{
	public static int GetDensityInRadius(
	this IResourceLayer resourceLayer,
	World world,
	CPos loc,
	int radius,
	Func<CPos, bool> isAllowedCell
	)
	{
		// Find all tiles within the specified radius
		var scanCells = world.Map.FindTilesInCircle(loc, radius);
		var totalDensity = 0;

		// Iterate through each cell in the scanned area
		foreach (var cell in scanCells)
		{
			// Skip cells that are not allowed
			if (!isAllowedCell(cell))
				continue;

			// Get the resource density for the current cell
			var resource = resourceLayer.GetResource(cell);
			totalDensity += resource.Density;
		}

		// Return the total density if it exceeds the threshold, otherwise return 0
		return totalDensity;
	}

	public static int GetMaxDensityInRadius(
		this IResourceLayer resourceLayer,
		World world,
		CPos loc,
		int radius,
		Func<CPos, bool> isAllowedCell
		)
	{
		// Find all tiles within the specified radius
		var scanCells = world.Map.FindTilesInCircle(loc, radius);
		var maxDensity = 0;
		var scanCellCount = scanCells.Count();

		// Iterate through each cell in the scanned area
		foreach (var cell in scanCells)
		{
			// Skip cells that are not allowed
			if (!isAllowedCell(cell))
				continue;

			// Get the maximum density for the resource type in the current cell
			var resource = resourceLayer.GetResource(cell);
			var maxCellDensity = resourceLayer.GetMaxDensity(resource.Type);

			// Update maxDensity if the current cell's max density is greater
			if (maxCellDensity > maxDensity)
				maxDensity = maxCellDensity;
		}

		// Return the maximum density multiplied by the number of scanned cells
		return maxDensity * scanCellCount;
	}

	public static double GetDensityRatioInRadius(
		this IResourceLayer resourceLayer,
		World world,
		CPos loc,
		int radius,
		Func<CPos, bool> isAllowedCell
		)
	{
		// Get the maximum density in the specified radius
		var maxDensity = resourceLayer.GetMaxDensityInRadius(world, loc, radius, isAllowedCell);

		// Get the total density in the specified radius
		var totalDensity = resourceLayer.GetDensityInRadius(world, loc, radius, isAllowedCell);

		// Return the ratio of total density to maximum density, or 0 if maxDensity is 0
		return maxDensity > 0 ? (double)totalDensity / maxDensity : 0f;
	}

}
