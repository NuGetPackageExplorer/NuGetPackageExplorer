using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NuGet.Frameworks
{
	/// <summary>
	/// Class NuGetTargetFrameworkItemExtensions.
	/// </summary>
	public static class NuGetTargetFrameworkItemExtensions
	{
		/// <summary>
		/// Ases the targeted platform.
		/// </summary>
		/// <param name="items">A collection of NuGetTargetFrameworkItems</param>
		/// <returns>A fully formated targeted platform path based on the collection.</returns>
		public static string AsTargetedPlatformPath(this ObservableCollection<NuGetTargetFrameworkItem> items)
		{
			var selectedPlatforms = (items.Where(platform => platform.IsSelected)
				.Select(platform => platform.SelectedItem?.Framework ?? platform?.Framework)).ToList();

			return selectedPlatforms.AsTargetedPlatformPath();
		}
	}
}