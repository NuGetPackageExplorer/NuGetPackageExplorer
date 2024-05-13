using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGetPackageExplorer.Types;
using NuGetPe;
using NuGetPe.AssemblyMetadata;
using PackageExplorerViewModel;

#if HAS_UNO || USE_WINUI
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Text;
using Uno.Extensions;
using Uno.Logging;

using TabControl = Microsoft.UI.Xaml.Controls.TabView;
using TabItem = Microsoft.UI.Xaml.Controls.TabViewItem;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
#endif

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

                var debugData = (selectedFile as PackageFile)?.DebugData;
                var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile.FileName);

                if (debugData == null)
                {
                    if (assemblyMetadata?.DebugData.HasDebugInfo == true)
                    {
                        debugData = assemblyMetadata.DebugData;
                    }
                }

                AssemblyDebugDataViewModel? debugDataViewModel = null;

                if (debugData != null)
                    debugDataViewModel = new AssemblyDebugDataViewModel(Task.FromResult(debugData));

#if !HAS_UNO && !USE_WINUI
                // No debug data to display
                if (assemblyMetadata != null && debugDataViewModel == null)
                {
                    var orderedAssemblyDataEntries = GetMetadataEntriesOrderedByImportance(assemblyMetadata);

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
                    var orderedAssemblyDataEntries = GetMetadataEntriesOrderedByImportance(assemblyMetadata);

                    // Tab control with three pages
                    var tc = new TabControl()
                    {
#if HAS_UNO || USE_WINUI
                        IsAddTabButtonVisible = false,
                        TabItems =
#else
                        Items =
#endif
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
                                Header = "PDB Info",
                                Content = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = new Controls.PdbInfoViewer
                                    {
                                        DataContext = debugDataViewModel
                                    }
                                }
                            },
                            new TabItem
                            {
                                Header = "PDB Sources",
                                Content = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = new Controls.PdbSourcesViewer
                                    {
                                        DataContext = debugDataViewModel
                                    }
                                }
                            }
                        }
                    };

                    return tc;
                }
#else
                // returning UIElement from here works.
                // however due to performance issues, we are just
                // returning the datacontext and letting the xaml to handle the view.
                // also, the ui layout is vastly different compared to the #if-block above
                return new AssemblyFileContent()
                {
                    Metadata = assemblyMetadata
                        ?.SelectOrDefault(GetMetadataEntriesOrderedByImportance)
                        .ToArray(),
                    DebugData = debugDataViewModel,
                };
#endif
            }
            catch (Exception e)
            {
#if HAS_UNO
                this.Log().Error("Failed to generate view", e);
#endif
            }

#if !HAS_UNO
            return new Grid();
#else
            // the empty object is needed for branching via type-checking
            return new AssemblyFileContent();
#endif
        }


        internal static Grid CreateAssemblyMetadataGrid(IEnumerable<KeyValuePair<string, string>> orderedAssemblyDataEntries)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

#if HAS_UNO || USE_WINUI
            var style = Application.Current.Resources["SelectableTextBlockLikeStyleWithoutTriggers"] as Style;
#else
            grid.SetBinding(Grid.MaxWidthProperty, new Binding(nameof(ScrollViewer.ActualWidth)) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ScrollViewer), 1) });
            var style = Application.Current.FindResource("SelectableTextBlockLikeStyleWithoutTriggers") as Style;
#endif

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
                    Style = style,
                    TextWrapping = TextWrapping.Wrap,
                };
                Grid.SetRow(value, grid.RowDefinitions.Count);
                Grid.SetColumn(value, 1);

                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.Children.Add(label);
                grid.Children.Add(value);
            }

            return grid;
        }


        /// <summary>
        /// Gets all the metadata entries sorted by importance
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> GetMetadataEntriesOrderedByImportance(AssemblyMetaDataInfo assemblyMetaData)
        {
            if (assemblyMetaData.FullName != null)
            {
                yield return KeyValuePair.Create("Full Name", assemblyMetaData.FullName);
            }
            if (assemblyMetaData.StrongName != null)
            {
                yield return KeyValuePair.Create("Strong Name", assemblyMetaData.StrongName);
            }

            foreach (var entry in assemblyMetaData.MetadataEntries.OrderBy(kv => kv.Key))
            {
                yield return entry;
            }

            if (assemblyMetaData.ReferencedAssemblies != null)
            {
                var assemblyNamesDelimitedByLineBreak = string.Join(
                    Environment.NewLine,
                    assemblyMetaData.ReferencedAssemblies
                        .OrderBy(assName => assName.Name)
                        .Select(assName => assName.FullName));

                yield return KeyValuePair.Create("Referenced assemblies", assemblyNamesDelimitedByLineBreak);
            }
        }

#if HAS_UNO || USE_WINUI
        [Bindable]
        public class AssemblyFileContent
        {
            public bool IsAssemblyFileContent => true;

            public KeyValuePair<string, string>[]? Metadata { get; init; }
            public AssemblyDebugDataViewModel? DebugData { get; init; }

            public override string ToString()
            {
                // workaround: prevent class name to be displayed during content transition
                return string.Empty;
            }
        }
#endif
    }
}
