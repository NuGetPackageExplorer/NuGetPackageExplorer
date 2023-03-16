using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NupkgExplorer.Framework.Extensions
{
    public static class CompositionContainerExtensions
    {
        public static object GetExportedValue(this CompositionContainer container, Type type)
        {
            var export = container.GetExports(type, null, null)
                .FirstOrDefault()
                ?? throw new ImportCardinalityMismatchException("Cannot find export for type:" + type);

            return export.Value;
        }
    }
}
