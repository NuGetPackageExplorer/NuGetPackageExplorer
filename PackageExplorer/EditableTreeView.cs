using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace PackageExplorer {
    public class EditableTreeView : TreeView {
        public EditableTreeView() {

        }

        protected override System.Windows.DependencyObject GetContainerForItemOverride() {
            return new EditableTreeViewItem();
        }
    }
}
