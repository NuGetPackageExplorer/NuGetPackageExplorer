// ***********************************************************************
// Assembly         : NuGetFrameworks
// Author           : Shawn
// Created          : 02-25-2016
//
// Last Modified By : Shawn
// Last Modified On : 02-28-2016
// ***********************************************************************
// <copyright file="NuGetExtensions.cs" company="">
//     Copyright ©  2016
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Frameworks
{
	/// <summary>
	/// Class Extensions.
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
		/// <returns>IEnumerable&lt;NuGetFramework&gt;.</returns>
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
		/// <returns>NuGetFramework.</returns>
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
		/// <param name="dotNetframeworkName">Name of the dot netframework.</param>
		/// <returns>NuGetFramework.</returns>
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
		/// <returns>IEnumerable&lt;NuGetFramework&gt;.</returns>
		public static IEnumerable<NuGetFramework> FindByIdentifierShortName(this IFrameworkMappings frameworkMapping, string frameworkIdentifier, bool includeEquivalentFrameworks = false)
		{
			var framework =
				frameworkMapping.IdentifierShortNames.FirstOrDefault(
					x =>
						string.Compare(x.Value, frameworkIdentifier, StringComparison.InvariantCultureIgnoreCase) == 0 ||
						frameworkIdentifier.StartsWith(x.Value, StringComparison.InvariantCultureIgnoreCase));

			if (framework.Key == null) return null;

			var matchIncludeVersion = Char.IsDigit(frameworkIdentifier, frameworkIdentifier.Length - 1);

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
		/// <returns>NuGetFramework.</returns>
		public static NuGetFramework FindByFrameworkName(this IFrameworkMappings frameworkMapping, string frameworkName)
		{
			var result = frameworkMapping.EquivalentFrameworks.FirstOrDefault(x =>
				string.Compare(x.Key.Framework, frameworkName, StringComparison.InvariantCultureIgnoreCase) == 0 ||
				string.Compare(x.Value.Framework, frameworkName, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result.Key;
		}

		/// <summary>
		/// Finds the name of the by dot net framework.
		/// </summary>
		/// <param name="frameworkMapping">The framework mapping.</param>
		/// <param name="dotNetframeworkName">Name of the dot netframework.</param>
		/// <returns>NuGetFramework.</returns>
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
		/// <returns>System.String.</returns>
		public static string FindShortNameByFramework(this IFrameworkMappings frameworkMapping,
			NuGetFramework framework)
		{
			var result = frameworkMapping.IdentifierShortNames.FirstOrDefault(x => string.Compare(x.Key, framework.Framework, StringComparison.InvariantCultureIgnoreCase) == 0);

			return result.Value;
		}

		#endregion Framework Mappings

		/// <summary>
		/// Finds the short name of the by identifier.
		/// </summary>
		/// <param name="frameworks">The frameworks.</param>
		/// <param name="identifierShortName">Short name of the identifier.</param>
		/// <returns>IEnumerable&lt;NuGetFramework&gt;.</returns>
		public static IEnumerable<NuGetFramework> FindByIdentifierShortName(this IEnumerable<NuGetFramework> frameworks, string identifierShortName)
		{
			if (!frameworks.Any() || string.IsNullOrEmpty(identifierShortName)) return null;

			var matchIncludeVersion = Char.IsDigit(identifierShortName, identifierShortName.Length - 1);

			var result =
				frameworks.Where(
					x =>
						string.Compare(x.ToShortName(matchIncludeVersion), identifierShortName,
							StringComparison.InvariantCultureIgnoreCase) == 0).ToList();

			return result;
		}

		#region Nuget Framework		
		/// <summary>
		/// To the short name.
		/// </summary>
		/// <param name="framework">The framework.</param>
		/// <param name="includeVersionInfo">if set to <c>true</c> [include version information].</param>
		/// <returns>System.String.</returns>
		public static string ToShortName(this NuGetFramework framework, bool includeVersionInfo)
		{
			if (framework == null) return null;

			var shortName = DefaultFrameworkMappings.Instance.FindShortNameByFramework(framework);

			string verString;

			if (framework.Version.Build != 0) verString = framework.Version.ToString(3);
			else if (framework.Version.Minor != 0) verString = framework.Version.ToString(2);
			else if (framework.Version.Major != 0) verString = framework.Version.ToString(1);
			else verString = framework.Version.ToString(0);

			return string.Format("{0}{1}", shortName, includeVersionInfo ? verString.Replace(".", "") : "");
		}
		#endregion Nuget Framework		
	}
}