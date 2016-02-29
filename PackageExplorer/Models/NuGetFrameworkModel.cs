// ***********************************************************************
// Assembly         : NuGetFrameworks
// Author           : Shawn
// Created          : 02-26-2016
//
// Last Modified By : Shawn
// Last Modified On : 02-28-2016
// ***********************************************************************
// <copyright file="NuGetFrameworkModel.cs" company="">
//     Copyright ©  2016
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using NuGet.Frameworks;

namespace NuGet.Frameworks
{
	/// <summary>
	/// Class NuGetFrameworkModel.
	/// </summary>
	[PropertyChanged.ImplementPropertyChanged]
	public class NuGetFrameworkModel
	{
		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance.</value>
		public static NuGetFrameworkModel Instance { get; } = new NuGetFrameworkModel();

		/// <summary>
		/// Gets or sets the frameworks.
		/// </summary>
		/// <value>The frameworks.</value>
		public IEnumerable<NuGetFramework> Frameworks { get; set; } = new List<NuGetFramework>();

		/// <summary>
		/// Gets the portable frameworks.
		/// </summary>
		/// <value>The portable frameworks.</value>
		public ObservableCollection<NuGetTargetFrameworkItem> PortableFrameworks { get; private set; } = new ObservableCollection<NuGetTargetFrameworkItem>();
		/// <summary>
		/// Initializes a new instance of the <see cref="NuGetFrameworkModel"/> class.
		/// </summary>
		public NuGetFrameworkModel()
		{
			Initialize();
		}

		/// <summary>
		/// Initializes this instance.
		/// </summary>
		public void Initialize()
		{
			//var t1 = NuGet.Frameworks.DefaultCompatibilityProvider.Instance;
			//var t2 = NuGet.Frameworks.DefaultFrameworkMappings.Instance;
			//var t3 = NuGet.Frameworks.DefaultFrameworkNameProvider.Instance;
			//var t4 = NuGet.Frameworks.DefaultPortableFrameworkMappings.Instance;

			// Lets pull a flat and unique list of all platforms that NuGet works with
			Frameworks =
				NuGet.Frameworks.DefaultPortableFrameworkMappings.Instance.ProfileFrameworks.SelectMany(x => x.Value)
					.Concat(
						NuGet.Frameworks.DefaultPortableFrameworkMappings.Instance.ProfileOptionalFrameworks.SelectMany(x => x.Value)).Distinct().OrderBy(x => x.DotNetFrameworkName).ToList();

			InitializePCLModel();
		}

		/// <summary>
		/// Initializes the PCL model.
		/// </summary>
		private void InitializePCLModel()
		{
			var frameWork = new NuGetTargetFrameworkItem { Name = ".NET Framework" };
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = ".NET Framework 4 and higher", ShortName = "net4", Framework = Frameworks.FindByIdentifierShortName("net40").FirstOrDefault() });
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = ".NET Framework 4.0.3 and higher", ShortName = "net403", Framework = Frameworks.FindByIdentifierShortName("net403").FirstOrDefault() });
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = ".NET Framework 4.5 and higher", ShortName = "net45", IsDefault = true, Framework = Frameworks.FindByIdentifierShortName("net45").FirstOrDefault() });
			frameWork.SelectedItem = frameWork.Items.FirstOrDefault(x => x.IsDefault);
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = "Silverlight" };
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = "Silverlight 4 and higher", ShortName = "sl4", Framework = Frameworks.FindByIdentifierShortName("sl4").FirstOrDefault() });
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = "Silverlight 5", ShortName = "sl5", IsDefault = true, Framework = Frameworks.FindByIdentifierShortName("sl5").FirstOrDefault() });
			frameWork.SelectedItem = frameWork.Items.FirstOrDefault(x => x.IsDefault);
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = "Windows Phone" };
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = "Windows Phone 7 and higher", ShortName = "wp7", Framework = Frameworks.FindByIdentifierShortName("wp7").FirstOrDefault() });
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = "Windows Phone 7.5 and higher", ShortName = "wp71", Framework = Frameworks.FindByIdentifierShortName("wp75").FirstOrDefault() });
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = "Windows Phone 8 and higher", ShortName = "wp8", Framework = Frameworks.FindByIdentifierShortName("wp8").FirstOrDefault() });
			frameWork.Items.Add(new NuGetTargetFrameworkItem { Name = "Windows Phone 8.1 and higher", ShortName = "wpa81", IsDefault = true, Framework = Frameworks.FindByIdentifierShortName("wpa81").FirstOrDefault() });
			frameWork.SelectedItem = frameWork.Items.FirstOrDefault(x => x.IsDefault);
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = ".NET for Windows Store apps", ShortName = "windows", Framework = Frameworks.FindByIdentifierShortName("windows").FirstOrDefault() };
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = "Xamarin For iOS", ShortName = "xamarinios", Framework = Frameworks.FindByIdentifierShortName("xamarinios").FirstOrDefault() };
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = "Xamarin For Android", ShortName = "monodroid", Framework = Frameworks.FindByIdentifierShortName("monodroid").FirstOrDefault() };
			PortableFrameworks.Add(frameWork);
		}
	}

	/// <summary>
	/// Class NuGetTargetFrameworkItem.
	/// </summary>
	[PropertyChanged.ImplementPropertyChanged]
	public class NuGetTargetFrameworkItem
	{
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the short name.
		/// </summary>
		/// <value>The short name.</value>
		public string ShortName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is default.
		/// </summary>
		/// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
		public bool IsDefault { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is selected.
		/// </summary>
		/// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
		public bool IsSelected { get; set; }

		/// <summary>
		/// Gets or sets the selected item.
		/// </summary>
		/// <value>The selected item.</value>
		public NuGetTargetFrameworkItem SelectedItem { get; set; }

		/// <summary>
		/// Gets or sets the framework.
		/// </summary>
		/// <value>The framework.</value>
		public NuGetFramework Framework { get; set; }

		/// <summary>
		/// Gets or sets the items.
		/// </summary>
		/// <value>The items.</value>
		public ObservableCollection<NuGetTargetFrameworkItem> Items { get; set; } = new ObservableCollection<NuGetTargetFrameworkItem>();

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>A <see cref="System.String" /> that represents this instance.</returns>
		public override string ToString()
		{
			return Name;
		}
	}

	/// <summary>
	/// Class NuGetTargetFrameworkItemExtensions.
	/// </summary>
	public static class NuGetTargetFrameworkItemExtensions
	{
		/// <summary>
		/// Ases the targeted platform.
		/// </summary>
		/// <param name="items">The items.</param>
		/// <returns>System.String.</returns>
		public static string AsTargetedPlatform(this ObservableCollection<NuGetTargetFrameworkItem> items)
		{
			var result = from platform in items
				where platform.IsSelected
				select platform.SelectedItem ?? platform;

			var str = result.Aggregate<NuGetTargetFrameworkItem, string>("", (a, b) => string.Format("{0}{1}{2}", a, a.Length > 0 ? "+" : "", b.ShortName));

			if (str.Length > 0)
			{
				str = "portable-" + str;
			}

			return str;
		}
	}
}
