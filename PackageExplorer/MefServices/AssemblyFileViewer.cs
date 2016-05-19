using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CodeExecutor;
using NuGetPackageExplorer.Types;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".dll", ".exe", ".winmd")]
    internal class AssemblyFileViewer : IPackageContentViewer
    {
        #region IPackageContentViewer Members

        public object GetView(string extension, Stream stream)
        {
            string tempFile = Path.GetTempFileName();
            using (FileStream fileStream = File.OpenWrite(tempFile))
            {
                stream.CopyTo(fileStream);
            }

            try
            {
                IDictionary<string, string> assemblyData = RemoteCodeExecutor.GetAssemblyMetadata(tempFile);

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                if (assemblyData != null)
                {
                    foreach (var data in assemblyData.OrderBy(d => d.Key))
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

                return grid;
            }
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
        }

        #endregion
    }
}