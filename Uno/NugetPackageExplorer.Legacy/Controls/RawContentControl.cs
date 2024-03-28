using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Uno.Extensions;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace NupkgExplorer.Controls
{
    public class RawContentControl : Border
    {
        #region DependencyProperty: RawContent

        public static DependencyProperty RawContentProperty { get; } = DependencyProperty.Register(
            nameof(RawContent),
            typeof(object),
            typeof(RawContentControl),
            new PropertyMetadata(default(object), (s, e) => ((RawContentControl)s).OnRawContentChanged(e)));

        public object RawContent
        {
            get => (object)GetValue(RawContentProperty);
            set => SetValue(RawContentProperty, value);
        }

        #endregion

        private void OnRawContentChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is UIElement el)
            {
                Child = el;
            }
            else if (e.NewValue != null)
            {
                Child = new TextBlock();
                Child.SetBinding(TextBlock.TextProperty, new Binding { Source = this, Path = nameof(RawContent) });
            }
        }
    }
}
