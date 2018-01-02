using System;
using System.ComponentModel.Composition;

namespace NuGetPackageExplorer.Types
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PackageCommandMetadataAttribute : ExportAttribute
    {
        private readonly string _text;

        public PackageCommandMetadataAttribute(string text) : base(typeof(IPackageCommand))
        {
            if (string.IsNullOrEmpty("text"))
            {
                throw new ArgumentNullException("text");
            }

            _text = text;
        }

        public string Text
        {
            get { return _text; }
        }
    }
}