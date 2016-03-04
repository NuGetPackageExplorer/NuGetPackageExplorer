using System.Windows.Input;
using NuGet.Frameworks;

namespace PackageExplorer.ViewModels
{
	public class PortableLibraryViewModel
	{
		public PortableLibraryViewModel()
		{
		}

		/// <summary>
		/// Gets the data model for all NuGet frameworks.
		/// </summary>
		/// <value>The model.</value>
		public NuGetFrameworkModel Model { get; } = new NuGetFrameworkModel();

		/// <summary>
		/// Gets or sets the save command.
		/// </summary>
		/// <value>The save command.</value>
		public ICommand SaveCommand { get; set; }

		/// <summary>
		/// Gets or sets the cancel command.
		/// </summary>
		/// <value>The cancel command.</value>
		public ICommand CancelCommand { get; set; }

		/// <summary>
		/// Gets the targeted framework string.
		/// </summary>
		/// <returns>System.String.</returns>
		public string GetTargetedFrameworkString()
		{
			var result = Model.SelectedFrameworks.AsTargetedPlatformPath();

			return result;
		}

		/// <summary>
		/// Validates the targeted framework string.
		/// </summary>
		/// <returns><c>true</c> if currently selected platforms represent a valid targeted framework path, <c>false</c> otherwise.</returns>
		public bool IsValidTargetedFrameworkPath()
		{
			return Model.SelectedFrameworks.AsTargetedPlatformPath().Length > 2;
		}
	}
}
