using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace NupkgExplorer.Views.Helpers
{
	public class VisualTreeHelperEx
	{
		public static T GetFirstDescendant<T>(DependencyObject reference) => GetDescendants(reference)
			.OfType<T>()
			.FirstOrDefault();

		public static T GetFirstDescendant<T>(DependencyObject reference, Func<T, bool> predicate) => GetDescendants(reference)
			.OfType<T>()
			.FirstOrDefault(predicate);

		public static IEnumerable<DependencyObject> GetDescendants(DependencyObject reference)
		{
			foreach (var child in GetChildren(reference))
			{
				yield return child;
				foreach (var grandchild in GetDescendants(child))
				{
					yield return grandchild;
				}
			}
		}

		public static IEnumerable<DependencyObject> GetChildren(DependencyObject reference)
		{
			return Enumerable
				.Range(0, VisualTreeHelper.GetChildrenCount(reference))
				.Select(x => VisualTreeHelper.GetChild(reference, x));
		}
	}
}
