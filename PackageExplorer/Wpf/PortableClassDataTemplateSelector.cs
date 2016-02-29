// ***********************************************************************
// Assembly         : NuGetPackageExplorer
// Author           : Shawn
// Created          : 02-24-2016
//
// Last Modified By : Shawn
// Last Modified On : 02-24-2016
// ***********************************************************************
// <summary></summary>
// ***********************************************************************
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NuGet.Frameworks;

namespace PackageExplorer.Wpf
{
	/// <summary>
	/// Class PortableClassDataTemplateSelector.
	/// </summary>
	public class PortableClassDataTemplateSelector : DataTemplateSelector
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
