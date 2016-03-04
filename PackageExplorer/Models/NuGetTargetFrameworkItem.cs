using System.Collections.ObjectModel;

namespace NuGet.Frameworks
{
	/// <summary>
	/// Class NuGetTargetFrameworkItem.
	/// </summary>
	[PropertyChanged.ImplementPropertyChanged]
	public class NuGetTargetFrameworkItem
	{
		/// <summary>
		/// Gets or sets the friendly/display name for this framework.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the simple short name for this item (ex: sl, wpa, win).
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
}