using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using NuGetPackageExplorer.Types;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace PackageExplorer
{
    public sealed partial class PackageViewer : UserControl
    {
        private readonly ISettingsManager _settings;
        private readonly IUIServices _messageBoxServices;

        public PackageViewer(ISettingsManager settings, IUIServices messageBoxServices, IPackageChooser packageChooser)
        {
            this.InitializeComponent();

            _settings = settings;
            _messageBoxServices = messageBoxServices;

        }
    }
}
