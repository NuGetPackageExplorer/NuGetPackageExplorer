using System;
using System.ComponentModel.Composition;
using System.IO;

using NuGetPackageExplorer.Types;

using NuGetPe;

using NupkgExplorer.Framework.Navigation;
using NupkgExplorer.Presentation.Content;

using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PackageExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Export]
    public sealed partial class Shell : Page
	{
        [Import]
        public NavigationService NavigationService { get; set; }

		public Shell()
		{
			InitializeComponent();
		}

        public Frame GetContentFrame() => ContentFrame;

		private void ToggleDarkLightTheme(object sender, RoutedEventArgs e)
		{
#if WINDOWS_UWP
			RequestedTheme = RequestedTheme == ElementTheme.Dark
				? ElementTheme.Light
				: ElementTheme.Dark;
#endif
		}

        private async void OpenLocalPackage(object sender, RoutedEventArgs e)
        {
            // TODO: move file interaction to DialogService or UIService
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(Constants.PackageExtension);
            picker.FileTypeFilter.Add(Constants.SymbolPackageExtension);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var vm = await InspectPackageViewModel.CreateFromLocalPackage(file);

                NavigationService.NavigateTo(vm);
            }
        }

        private void OpenRemotePackage(object sender, RoutedEventArgs e)
        {
            NavigationService.NavigateTo<FeedPackagePickerViewModel>();
        }

        private void ShowLandingPage(object sender, RoutedEventArgs e)
        {
            NavigationService.NavigateTo<FeedPackagePickerViewModel>();
        }
	}
}
