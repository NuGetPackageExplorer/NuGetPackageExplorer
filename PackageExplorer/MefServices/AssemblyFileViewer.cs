using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;
using NuGetPe;
using NuGetPe.AssemblyMetadata;
using PackageExplorerViewModel;
using PackageExplorerViewModel.Utilities;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".dll", ".exe", ".winmd", SupportsWindows10S = false)]
    internal class AssemblyFileViewer : IPackageContentViewer
    {

        public object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles)
        {
            DiagnosticsClient.TrackEvent("AssemblyFileViewer");

            

            try
            {
                using var str = selectedFile.GetStream();
                using var tempFile = new TemporaryFile(str);

                var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile.FileName);
                AssemblyDebugDataViewModel? debugDataViewModel = null;
                if (assemblyMetadata?.DebugData.HasDebugInfo == true)
                    debugDataViewModel = new AssemblyDebugDataViewModel(Task.FromResult(assemblyMetadata.DebugData));

                // No debug data to display
                if (assemblyMetadata != null && debugDataViewModel == null)
                {
                    var orderedAssemblyDataEntries = assemblyMetadata.GetMetadataEntriesOrderedByImportance();

                    var grid = CreateAssemblyMetadataGrid(orderedAssemblyDataEntries);

                    return new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = grid,
                    };
                }
                else if (assemblyMetadata != null && debugDataViewModel != null)
                {
                    var orderedAssemblyDataEntries = assemblyMetadata.GetMetadataEntriesOrderedByImportance();

                    // Tab control with two pages
                    var tc = new TabControl()
                    {
                        Items =
                        {
                            new TabItem
                            {
                                Header = "Assembly Attributes",
                                Content = new ScrollViewer()
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = CreateAssemblyMetadataGrid(orderedAssemblyDataEntries)
                                }
                            },
                            new TabItem
                            {
                                Header = "Embedded PDB Data",
                                Content = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = new Controls.PdbFileViewer
                                    {
                                        DataContext = debugDataViewModel
                                    }
                                }
                            }
                        }
                    };

                    return tc;
                }
            }
            catch { }

            return new Grid();
        }


        private static Grid CreateAssemblyMetadataGrid(IEnumerable<KeyValuePair<string, string>> orderedAssemblyDataEntries)
        {

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var style =  Application.Current.FindResource("SelectableTextBlockLikeStyleWithoutTriggers") as Style;

            foreach (var data in orderedAssemblyDataEntries)
            {
                var label = new TextBox
                {
                    Text = data.Key + ':',
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(3, 3, 10, 0),
                    Style = style
                };
                Grid.SetRow(label, grid.RowDefinitions.Count);
                Grid.SetColumn(label, 0);

                var value = new TextBox
                {
                    Text = data.Value,
                    Margin = new Thickness(0, 3, 3, 0),
                    Style = style
                };
                Grid.SetRow(value, grid.RowDefinitions.Count);
                Grid.SetColumn(value, 1);

                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.Children.Add(label);
                grid.Children.Add(value);
            }

            return grid;
        }
    }
}
