using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NuGet.Frameworks;

namespace PackageExplorer.Wpf
{
	/// <summary>
	/// This DataTemplateSelector allows for dynamically switching between templates in a databound control (like a ListView).  The main
	/// use for this to switch between a single item checkbox and a dropdown list based on if there is 1 primary NuGetTargetFrame work or multiple
	/// For an example of useage see <see cref="PortableLibraryDialog"/>
	/// </summary>
	public class NuGetTargetFrameworkItemDataTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Gets or sets the platform data template.
		/// </summary>
		/// <value>The platform data template.</value>
		public DataTemplate PlatformDataTemplate { get; set; }

		/// <summary>
		/// Gets or sets the platform selection data template.
		/// </summary>
		/// <value>The platform selection data template.</value>
		public DataTemplate PlatformSelectionDataTemplate { get; set; }

		/// <summary>
		/// Selects the template based on if the portable class item has any child objects.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="container">The container.</param>
		/// <returns>DataTemplate.</returns>
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var element = container as FrameworkElement;
			var portableClassItem = item as NuGetTargetFrameworkItem;

			if (element != null && portableClassItem != null)
			{
				return portableClassItem.Items.Any() ? PlatformSelectionDataTemplate : PlatformDataTemplate;
			}

			return null;
		}
	}
}
