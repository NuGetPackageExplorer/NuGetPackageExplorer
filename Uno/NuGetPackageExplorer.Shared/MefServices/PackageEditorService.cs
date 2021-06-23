using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

using NuGetPackageExplorer.Types;

namespace NuGetPackageExplorer.MefServices
{
    [Export(typeof(IPackageEditorService))]
    class PackageEditorService : IPackageEditorService
    {
        void IPackageEditorService.BeginEdit()
        {
            throw new NotImplementedException();
        }

        void IPackageEditorService.CancelEdit()
        {
            throw new NotImplementedException();
        }

        bool IPackageEditorService.CommitEdit()
        {
            throw new NotImplementedException();
        }
    }
}
