// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NuGetPackageExplorer.Types;
using SuperKinhLuan.SyntaxHighlighting;

namespace PackageExplorer
{
    /// <summary>
    /// A simple text control for displaying syntax highlighted source code.
    /// </summary>
    public class SyntaxHighlightingTextBox : RichTextBox
    {
        #region public SourceLanguageType SourceLanguage
        /// <summary>
        /// Gets or sets the source language type.
        /// </summary>
        public SourceLanguageType SourceLanguage
        {
            get { return (SourceLanguageType)GetValue(SourceLanguageProperty); }
            set { SetValue(SourceLanguageProperty, value); }
        }

        /// <summary>
        /// Identifies the SourceLanguage dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceLanguageProperty =
            DependencyProperty.Register(
                "SourceLanguage",
                typeof(SourceLanguageType),
                typeof(SyntaxHighlightingTextBox),
                new PropertyMetadata(SourceLanguageType.Text, new PropertyChangedCallback(OnPropertyChanged)));

        #endregion public SourceLanguageType SourceLanguage

        #region public string SourceCode
        /// <summary>
        /// Gets or sets the source code to display inside the syntax
        /// highlighting text block.
        /// </summary>
        public string SourceCode
        {
            get { return GetValue(SourceCodeProperty) as string; }
            set { SetValue(SourceCodeProperty, value); }
        }

        /// <summary>
        /// Identifies the SourceCode dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceCodeProperty =
            DependencyProperty.Register(
                "SourceCode",
                typeof(string),
                typeof(SyntaxHighlightingTextBox),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPropertyChanged)));
        
        #endregion public string SourceCode

        private static void OnPropertyChanged(object sender, DependencyPropertyChangedEventArgs args) {
            ((SyntaxHighlightingTextBox)sender)._propertyChanged = true;
        }

        private bool _propertyChanged;

        /// <summary>
        /// Initializes a new instance of the SyntaxHighlightingTextBlock
        /// control.
        /// </summary>
        public SyntaxHighlightingTextBox()
        {
            IsReadOnly = true;
            Document = new FlowDocument();
            Document.PageWidth = 1000;
        }

        /// <summary>
        /// Clears and updates the contents.
        /// </summary>
        public void Reparse()
        {
            // if no property has changed since the last parse, don't bother to reparse it.
            if (!_propertyChanged) {
                return;
            }

            SyntaxHighlighter.Highlight(
                SourceCode, 
                Document, 
                CreateLanguageInstance(SourceLanguage),
                "Loading and parsing content...");

            ScrollToHome();
            _propertyChanged = false;
        }

        /// <summary>
        /// Retrieves the language instance used by the highlighting system.
        /// </summary>
        /// <param name="type">The language type to create.</param>
        /// <returns>Returns a new instance of the language parser.</returns>
        private static ILanguage CreateLanguageInstance(SourceLanguageType type)
        {
            switch (type)
            {
                case SourceLanguageType.Text:
                    return Languages.PlainText;

                case SourceLanguageType.Asax:
                    return Languages.Asax;

                case SourceLanguageType.Ashx:
                    return Languages.Ashx;

                case SourceLanguageType.Aspx:
                    return Languages.Aspx;

                case SourceLanguageType.AspxCSharp:
                    return Languages.AspxCs;

                case SourceLanguageType.AspxVisualBasic:
                    return Languages.AspxVb;

                case SourceLanguageType.Css:
                    return Languages.Css;
                
                case SourceLanguageType.Html:
                    return Languages.Html;

                case SourceLanguageType.Php:
                    return Languages.Php;

                case SourceLanguageType.PowerShell:
                    return Languages.PowerShell;

                case SourceLanguageType.Sql:
                    return Languages.Sql;

                case SourceLanguageType.CSharp:
                    return Languages.CSharp;
                    
                case SourceLanguageType.Cpp:
                    return Languages.Cpp;

                case SourceLanguageType.JavaScript:
                    return Languages.JavaScript;

                case SourceLanguageType.VisualBasic:
                    return Languages.VbDotNet;

                case SourceLanguageType.Xaml:
                case SourceLanguageType.Xml:
                    return Languages.Xml;

                default:
                    throw new InvalidOperationException("Could not locate the provider.");
            }
        }
    }
}