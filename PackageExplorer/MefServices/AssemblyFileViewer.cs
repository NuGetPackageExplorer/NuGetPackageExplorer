using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".dll", ".exe", ".winmd", SupportsWindows10S = false)]
    internal class AssemblyFileViewer : IPackageContentViewer
    {
        #region IPackageContentViewer Members

        public object GetView(string extension, Stream stream)
        {
            var tempFile = Path.GetTempFileName();

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            try
            {
                using (var fileStream = File.OpenWrite(tempFile))
                {
                    stream.CopyTo(fileStream);
                }

                var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile);


                if (assemblyMetadata != null)
                {
                    var orderedAssemblyDataEntries = assemblyMetadata.GetMetadataEntriesOrderedByImportance();

                    foreach (var data in orderedAssemblyDataEntries)
                    {
                        var label = new TextBlock
                        {
                            Text = data.Key + ':',
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(3, 3, 10, 0)
                        };
                        Grid.SetRow(label, grid.RowDefinitions.Count);
                        Grid.SetColumn(label, 0);

                        var value = new TextBlock
                        {
                            Text = data.Value,
                            Margin = new Thickness(0, 3, 3, 0)
                        };
                        Grid.SetRow(value, grid.RowDefinitions.Count);
                        Grid.SetColumn(value, 1);

                        grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1, GridUnitType.Auto)});
                        grid.Children.Add(label);
                        grid.Children.Add(value);
                    }
                }
            }
            catch { }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch
                    {
                    }
                }
            }

            return grid;
        }

        #endregion
    }
}
