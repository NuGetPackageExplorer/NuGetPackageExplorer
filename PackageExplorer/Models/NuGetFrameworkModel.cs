using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
		/// Gets the selected frameworks.
		/// </summary>
		/// <value>The selected frameworks.</value>
		public IEnumerable<NuGetFramework> SelectedFrameworks
		{
			get
			{
				var selectedPlatforms = (from platform in PortableFrameworks
										 where platform.IsSelected
										 select platform.SelectedItem?.Framework ?? platform?.Framework);

				return selectedPlatforms;
			}
		}
		/// <summary>
		/// Gets a value indicating whether this instance is selection valid.
		/// </summary>
		/// <value><c>true</c> if this instance is selection valid; otherwise, <c>false</c>.</value>
		public bool IsSelectionValid
		{
			get { return SelectedFrameworks.IsValidTargetPlatform(); }
		}

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
			// There are 4 parimary static instances within the NuGet Framework that we can leverage
			// These include the following
			//	var t1 = NuGet.Frameworks.DefaultCompatibilityProvider.Instance;
			//	var t2 = NuGet.Frameworks.DefaultFrameworkMappings.Instance;
			//	var t3 = NuGet.Frameworks.DefaultFrameworkNameProvider.Instance;
			//	var t4 = NuGet.Frameworks.DefaultPortableFrameworkMappings.Instance;

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
		/// <remarks>
		/// Note: For now this is a hardcoded list.  Ideally this can be dynamically build from the NuGet libraries in the near future
		/// </remarks>
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

			frameWork = new NuGetTargetFrameworkItem { Name = ".NET for Windows Store apps", ShortName = "win8", Framework = Frameworks.FindByIdentifierShortName("win8").FirstOrDefault() };
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = "Xamarin For iOS", ShortName = "xamarinios", Framework = Frameworks.FindByIdentifierShortName("xamarinios").FirstOrDefault() };
			PortableFrameworks.Add(frameWork);

			frameWork = new NuGetTargetFrameworkItem { Name = "Xamarin For Android", ShortName = "MonoAndroid", Framework = Frameworks.FindByIdentifierShortName("MonoAndroid").FirstOrDefault() };
			PortableFrameworks.Add(frameWork);
		}
	}
}
