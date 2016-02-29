using NuGet.Frameworks;

namespace PackageExplorer.ViewModels
{
	public class PortableLibraryViewModel
	{
		public NuGetFrameworkModel Model { get; } = new NuGetFrameworkModel();		
	}
}
