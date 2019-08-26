using System.Windows;
using System.Windows.Controls;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public class PackageListBoxStyleSelector : StyleSelector
    {
        public Style? PackageItemStyle { get; set; }
        public Style? LoadingStatusIndicatorStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is PackageChooserViewModel)
            {
                return LoadingStatusIndicatorStyle!;
            }
            return PackageItemStyle!;
        }
    }
}
