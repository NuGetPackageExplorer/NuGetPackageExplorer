using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Extensions.DependencyInjection;
using Uno.Foundation;
using Windows.Foundation;
using Windows.UI.Xaml;
using NupkgExplorer.Framework.MVVM;
using NupkgExplorer.Framework.Navigation;
using NupkgExplorer.Presentation.Content;
using System.Reflection.Emit;

namespace NupkgExplorer.Presentation
{
    public class ShellViewModel : ViewModelBase
    {
        public ViewModelBase ActiveContent { get => GetProperty<ViewModelBase>(); set => SetProperty(value); }

        public bool IsStandaloneWindow
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        public Size WindowSize
        {
            get => GetProperty<Size>();
            set => SetProperty(value);
        }

        public ShellViewModel()
        {

#if __WASM__
            WindowSize = new Size(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
            IsStandaloneWindow = IsWindowStandalone();
            Window.Current.SizeChanged += (s, e) =>
            {
                var newSize = new Size(e.Size.Width, e.Size.Height);

                if (Math.Abs(newSize.Width - WindowSize.Width) > 10 || Math.Abs(newSize.Height - WindowSize.Height) > 10)
                {
                    IsStandaloneWindow = IsWindowStandalone();
                }
                WindowSize = newSize;
            };
#else
            IsStandaloneWindow = true;
#endif
        }

#if __WASM__
        private bool IsWindowStandalone()
        {
            return WebAssemblyRuntime.InvokeJS(@"window.matchMedia('(display-mode: standalone)').matches") == "true";
        }
#endif
    }
}
