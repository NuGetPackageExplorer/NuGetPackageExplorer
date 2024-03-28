#if USE_MONACO_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

using Monaco;

using Uno.Disposables;
using Uno.Extensions;

using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI;

namespace NupkgExplorer.Views.Extensions
{
    public static class CodeEditorExtensions
    {
        /* AutoLayoutOnResize: force layout on SizeChanged
         * CodeLanguage: expose CodeEditor.CodeLanguage for binding
         * ModelLanguage: [set-only] dp for direct monaco.editor.setModelLanguage call
         * AutoUpdateTheme: make editor use the appropriate theme
         * AutoUpdateThemeSubscription: [private] disposable for unhooking subscribed event
         */

        #region DependencyProperty: AutoLayoutOnResize

        public static DependencyProperty AutoLayoutOnResizeProperty { get; } = DependencyProperty.RegisterAttached(
            "AutoLayoutOnResize",
            typeof(bool),
            typeof(CodeEditorExtensions),
            new PropertyMetadata(default(bool), (d, e) => d.Maybe<CodeEditor>(control => OnAutoLayoutOnResizeChanged(control, e))));

        public static bool GetAutoLayoutOnResize(CodeEditor obj) => (bool)obj.GetValue(AutoLayoutOnResizeProperty);
        public static void SetAutoLayoutOnResize(CodeEditor obj, bool value) => obj.SetValue(AutoLayoutOnResizeProperty, value);

        #endregion
        #region DependencyProperty: CodeLanguage

        public static DependencyProperty CodeLanguageProperty { get; } = DependencyProperty.RegisterAttached(
            "CodeLanguage",
            typeof(string),
            typeof(CodeEditorExtensions),
            new PropertyMetadata(default(string), (d, e) => d.Maybe<CodeEditor>(control => OnCodeLanguageChanged(control, e))));

        public static string GetCodeLanguage(CodeEditor obj) => (string)obj.GetValue(CodeLanguageProperty);
        public static void SetCodeLanguage(CodeEditor obj, string value) => obj.SetValue(CodeLanguageProperty, value);

        #endregion
        #region DependencyProperty: ModelLanguage

        public static DependencyProperty ModelLanguageProperty { get; } = DependencyProperty.RegisterAttached(
            "ModelLanguage",
            typeof(string),
            typeof(CodeEditorExtensions),
            new PropertyMetadata(default(string), (d, e) => d.Maybe<CodeEditor>(control => OnModelLanguageChanged(control, e))));

        //public static string GetModelLanguage(CodeEditor obj) => (string)obj.GetValue(ModelLanguageProperty);
        public static void SetModelLanguage(CodeEditor obj, string value) => obj.SetValue(ModelLanguageProperty, value);

        #endregion
        #region DependencyProperty: AutoUpdateTheme

        public static DependencyProperty AutoUpdateThemeProperty { get; } = DependencyProperty.RegisterAttached(
            "AutoUpdateTheme",
            typeof(bool),
            typeof(CodeEditorExtensions),
            new PropertyMetadata(default(bool), (d, e) => d.Maybe<CodeEditor>(control => OnAutoUpdateThemeChanged(control, e))));

        public static bool GetAutoUpdateTheme(CodeEditor obj) => (bool)obj.GetValue(AutoUpdateThemeProperty);
        public static void SetAutoUpdateTheme(CodeEditor obj, bool value) => obj.SetValue(AutoUpdateThemeProperty, value);

        #endregion
        #region DependencyProperty: AutoUpdateThemeSubscription

        public static DependencyProperty AutoUpdateThemeSubscriptionProperty { get; } = DependencyProperty.RegisterAttached(
            "AutoUpdateThemeSubscription",
            typeof(IDisposable),
            typeof(CodeEditorExtensions),
            new PropertyMetadata(default(IDisposable)));

        public static IDisposable GetAutoUpdateThemeSubscription(CodeEditor obj) => (IDisposable)obj.GetValue(AutoUpdateThemeSubscriptionProperty);
        public static void SetAutoUpdateThemeSubscription(CodeEditor obj, IDisposable value) => obj.SetValue(AutoUpdateThemeSubscriptionProperty, value);

        #endregion

        private static readonly UISettings _uiSettings = new UISettings();

        private static void OnAutoLayoutOnResizeChanged(CodeEditor control, DependencyPropertyChangedEventArgs e)
        {
            control.SizeChanged -= ForceLayout;
            if (e.NewValue is bool value && value)
            {
                control.SizeChanged += ForceLayout;
            }

            void ForceLayout(object sender, SizeChangedEventArgs args)
            {
                if (sender is CodeEditor editor)
                {
                    // force layout on SizeChanged, or otherwise
                    // the editor would be stuck at minimal size when its visibility is toggled
                    editor.ExecuteJavascript("editor.layout();");
                }
            }
        }

        private static void OnCodeLanguageChanged(CodeEditor control, DependencyPropertyChangedEventArgs e)
        {
            var language = e.NewValue as string ?? "plaintext";

            // CodeEditor::CodeLanguageProperty is internal...
            control.CodeLanguage = language;
        }
        private static void OnModelLanguageChanged(CodeEditor control, DependencyPropertyChangedEventArgs e)
        {
            var language = e.NewValue as string ?? "plaintext";

            // CodeLanguage doesn't work when the control is loading/first loaded
            // calling its underlying method to ensure the language is actually set
            control.ExecuteJavascript($"monaco.editor.setModelLanguage(model, '{language}');");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Intended to be dispose on next call")]
        private static void OnAutoUpdateThemeChanged(CodeEditor control, DependencyPropertyChangedEventArgs e)
        {
            GetAutoUpdateThemeSubscription(control)?.Dispose();
            if (e.NewValue is bool value && value)
            {
                _uiSettings.ColorValuesChanged += OnColorValuesChanged;

                // force an initial update
                OnColorValuesChanged(_uiSettings, null!);

                SetAutoUpdateThemeSubscription(control, Disposable.Create(() =>
                    _uiSettings.ColorValuesChanged -= OnColorValuesChanged
                ));

                void OnColorValuesChanged(UISettings sender, object args)
                {
                    control.RequestedTheme = (Window.Current.Content as FrameworkElement)?.ActualTheme switch
                    {
                        ElementTheme actualTheme when (actualTheme != ElementTheme.Default) => actualTheme,
                        _ => sender.GetColorValue(UIColorType.Background) == Colors.Black
                            ? ElementTheme.Dark
                            : ElementTheme.Light,
                    };
                }
            }
        }

    }
}
#endif
