using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Frameworks
{
	/// <summary>
	/// Class for Extensions related to NuGetFramework objects.
	/// </summary>
	public static class NuGetFrameworkExtensions
	{
		#region Portable Framework Mappings
		/// <summary>
		/// Finds the short name of the by identifier.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="frameworkIdentifier">The framework identifier.</param>
		/// <param name="includeEquivalentFrameworks">if set to <c>true</c> [include equivalent frameworks].</param>
		/// <returns>A collection of NuGetFramework items that match the framework specified.</returns>
		public static IEnumerable<NuGetFramework> FindByIdentifierShortName(this IPortableFrameworkMappings frameworkMapping,
			string frameworkIdentifier, bool includeEquivalentFrameworks = false)
		{
			var frameworkName =
				DefaultFrameworkMappings.Instance.IdentifierShortNames.FirstOrDefault(
					x =>
						string.Compare(x.Value, frameworkIdentifier, StringComparison.InvariantCultureIgnoreCase) == 0 ||
						frameworkIdentifier.StartsWith(x.Value, StringComparison.InvariantCultureIgnoreCase));

			if (frameworkName.Key == null) return null;

			var frameworks =
				frameworkMapping.ProfileFrameworks.SelectMany(x => x.Value).Where(
					x =>
						string.Compare(x.Framework, frameworkName.Key, StringComparison.InvariantCultureIgnoreCase) == 0 ||
						string.Compare(x.Framework, frameworkName.Value, StringComparison.InvariantCultureIgnoreCase) == 0)
					.Concat(
						frameworkMapping.ProfileOptionalFrameworks.SelectMany(x => x.Value)
							.Where(
								x =>
									string.Compare(x.Framework, frameworkName.Key, StringComparison.InvariantCultureIgnoreCase) == 0 ||
									string.Compare(x.Framework, frameworkName.Value, StringComparison.InvariantCultureIgnoreCase) == 0));

			return frameworks.Distinct();
		}

		/// <summary>
		/// Finds the name of the by framework.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="frameworkName">Name of the framework.</param>
		/// <returns>A NuGetFramework that matches the framework name or null</returns>
		public static NuGetFramework FindByFrameworkName(this IPortableFrameworkMappings frameworkMapping, string frameworkName)
		{
			var result = frameworkMapping.ProfileFrameworks.SelectMany(x => x.Value).FirstOrDefault(x =>
				string.Compare(x.Framework, frameworkName, StringComparison.InvariantCultureIgnoreCase) == 0) ??
						 frameworkMapping.ProfileOptionalFrameworks.SelectMany(x => x.Value).FirstOrDefault(x =>
							 string.Compare(x.Framework, frameworkName, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result;
		}

		/// <summary>
		/// Finds the name of the by dot net framework.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="dotNetframeworkName">Name of the dot netframework (ex: "WindowsPhone, Version=8.1").</param>
		/// <returns>A NuGetFramework that matches the full .NET framework name or null</returns>
		public static NuGetFramework FindByDotNetFrameworkName(this IPortableFrameworkMappings frameworkMapping, string dotNetframeworkName)
		{
			var result = frameworkMapping.ProfileFrameworks.SelectMany(x => x.Value).FirstOrDefault(x =>
				string.Compare(x.DotNetFrameworkName, dotNetframeworkName, StringComparison.InvariantCultureIgnoreCase) == 0) ??
						 frameworkMapping.ProfileOptionalFrameworks.SelectMany(x => x.Value).FirstOrDefault(x =>
							 string.Compare(x.DotNetFrameworkName, dotNetframeworkName, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result;
		}
		#endregion Portable Framework Mappings

		#region Framework Mappings
		/// <summary>
		/// Finds the short name of the by identifier.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="frameworkIdentifier">The framework identifier.</param>
		/// <param name="includeEquivalentFrameworks">if set to <c>true</c> [include equivalent frameworks].</param>
		/// <returns>A collection of NuGetFramework items that match the framework specified.</returns>
		public static IEnumerable<NuGetFramework> FindByIdentifierShortName(this IFrameworkMappings frameworkMapping, string frameworkIdentifier, bool includeEquivalentFrameworks = false)
		{
			var framework =
				frameworkMapping.IdentifierShortNames.FirstOrDefault(
					x =>
						string.Compare(x.Value, frameworkIdentifier, StringComparison.InvariantCultureIgnoreCase) == 0 ||
						frameworkIdentifier.StartsWith(x.Value, StringComparison.InvariantCultureIgnoreCase));

			if (framework.Key == null) return null;

			var matchIncludeVersion = char.IsDigit(frameworkIdentifier, frameworkIdentifier.Length - 1);

			var result =
				frameworkMapping.EquivalentFrameworks.Where(
					x =>
						(string.Compare(x.Key.Framework, framework.Key, StringComparison.InvariantCultureIgnoreCase) == 0 &&
						 string.Compare(x.Key.ToShortName(matchIncludeVersion), frameworkIdentifier,
							 StringComparison.InvariantCultureIgnoreCase) == 0) ||
						string.Compare(x.Value.Framework, framework.Key, StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(x.Value.ToShortName(matchIncludeVersion), frameworkIdentifier,
							StringComparison.InvariantCultureIgnoreCase) == 0)
					.Select(x => x).ToList();

			if (includeEquivalentFrameworks)
			{
				return result.Select(x => x.Key).Concat(result.Select(x => x.Value));
			}

			return result.Select(x => x.Value);
		}

		/// <summary>
		/// Finds the name of the by framework.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="frameworkName">Name of the framework.</param>
		/// <returns>The NuGetFramework found (if any)</returns>
		public static NuGetFramework FindByFrameworkName(this IFrameworkMappings frameworkMapping, string frameworkName)
		{
			var result = frameworkMapping.EquivalentFrameworks.FirstOrDefault(x =>
				string.Compare(x.Key.Framework, frameworkName, StringComparison.InvariantCultureIgnoreCase) == 0 ||
				string.Compare(x.Value.Framework, frameworkName, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result.Key;
		}

		/// <summary>
		/// Finds the name based on the full .NET framework name. For Example: WindowsPhone would return the first matching framework in the collection with that name
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="dotNetframeworkName">Name of the dot netframework.</param>
		/// <returns>The NuGetFramework found (if any)</returns>
		public static NuGetFramework FindByDotNetFrameworkName(this IFrameworkMappings frameworkMapping, string dotNetframeworkName)
		{
			var result = frameworkMapping.EquivalentFrameworks.FirstOrDefault(x =>
				string.Compare(x.Key.DotNetFrameworkName, dotNetframeworkName, StringComparison.InvariantCultureIgnoreCase) == 0 ||
			string.Compare(x.Value.DotNetFrameworkName, dotNetframeworkName, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result.Key;
		}

		/// <summary>
		/// Finds the short name by framework.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="framework">The framework.</param>
		/// <returns>The shortname for the framework</returns>
		public static string FindShortNameByFramework(this IFrameworkMappings frameworkMapping,
			NuGetFramework framework)
		{
			var result = frameworkMapping.IdentifierShortNames.FirstOrDefault(x => string.Compare(x.Key, framework.Framework, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result.Value;
		}

		/// <summary>
		/// Finds the short name of the by short name identifier.  Example: Searching for sl4 would return the framework for SilverLight, Version=4.0
		/// </summary>
		/// <param name="frameworks">The frameworks.</param>
		/// <param name="identifierShortName">Short name of the identifier.</param>
		/// <returns>IEnumerable&lt;NuGetFramework&gt;.</returns>
		public static IEnumerable<NuGetFramework> FindByIdentifierShortName(this IEnumerable<NuGetFramework> frameworks, string identifierShortName)
		{
			if (!frameworks.Any() || string.IsNullOrEmpty(identifierShortName)) return null;

			var matchIncludeVersion = char.IsDigit(identifierShortName, identifierShortName.Length - 1);

			var result =
				frameworks.Where(
					x =>
						string.Compare(x.ToShortName(matchIncludeVersion), identifierShortName,
							StringComparison.InvariantCultureIgnoreCase) == 0).ToList();

			return result;
		}

		/// <summary>
		/// Ases the targeted platform path.
		/// </summary>
		/// <param name="items">The items.</param>
		/// <returns>A valud target platform path based on the NuGetFramwork items</returns>
		public static string AsTargetedPlatformPath(this IEnumerable<NuGetFramework> items)
		{
			if (!items.Any()) return string.Empty;

			// Lets reduce the list of frameworks down to its base amount (remove all duplicate/equilivant frameworks)
			var frameworkReducer = new FrameworkReducer();
			var workingPlatforms = frameworkReducer.Reduce(items);
			if (workingPlatforms == null || !workingPlatforms.Any()) workingPlatforms = items;

			if (!workingPlatforms.Any())
			{
				return string.Empty;
			}

			var str = string.Join("+", workingPlatforms.Select(x => x.GetShortFolderName()));

			if (!string.IsNullOrEmpty(str) && !str.Contains("portable-"))
			{
				str = "portable-" + str;
			}

			return str;
		}

		/// <summary>
		/// Determines whether the list of NuGetFrameworks are valid or not.
		/// </summary>
		/// <param name="frameworks">The frameworks.</param>
		/// <returns><c>true</c> if [is valid target platform] [the specified frameworks]; otherwise, <c>false</c>.</returns>
		public static bool IsValidTargetPlatform(this IEnumerable<NuGetFramework> frameworks)
		{
			if (!frameworks.Any()) return false; // If we don't have any frameworks, its not valid
			if (frameworks.Count() == 1) return true; // if we only have one framework, its always valid

			// This is a work in progress to try to "validate" a target platform string before building it
			var frameworkNameProvier = new FrameworkNameProvider(new[] { DefaultFrameworkMappings.Instance },
				new[] { DefaultPortableFrameworkMappings.Instance });

			int profileNumber;
			if (!frameworkNameProvier.TryGetPortableProfile(frameworks, out profileNumber) && frameworks.Count() > 1)
			{
				return false; // not a valid combination
			}

			if (profileNumber != -1)
			{
				frameworkNameProvier.TryGetPortableFrameworks(profileNumber, out frameworks);
			}

			return frameworks.Any();
		}

		#endregion Framework Mappings

		#region Nuget Framework		
		/// <summary>
		/// Returns the short name of a framework.  For example: SilverLight, Version=4.0 -> sl40 or WindowsPhone, Version=8.1 -> wpa81
		/// </summary>
		/// <param name="framework">The framework.</param>
		/// <param name="includeVersionInfo">if set to <c>true</c> [include version information].</param>
		/// <returns>The shortname for the framework including version information if requested</returns>
		public static string ToShortName(this NuGetFramework framework, bool includeVersionInfo)
		{
			if (framework == null) return null;

			if (includeVersionInfo)
			{
				return framework.GetShortFolderName();
			}

			var frameworkNameProvider = new FrameworkNameProvider(new[] {DefaultFrameworkMappings.Instance},
				new[] {DefaultPortableFrameworkMappings.Instance});

			string shortName;
			if (!frameworkNameProvider.TryGetShortIdentifier(framework.Framework, out shortName))
			{
				shortName = DefaultFrameworkMappings.Instance.FindShortNameByFramework(framework);
			}
			
			return shortName;
		}
		#endregion Nuget Framework		
	}
}