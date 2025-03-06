using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NuGetPeGenerators;

[Generator]
public class ResXCompositeFormatGenerator : IIncrementalGenerator
{
    private static readonly Regex InterpolationRegex = new Regex(@"(?<!\\){[0-9]+.*?}", RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var resxFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(resxFiles, static (spc, source) =>
        {
            var ((resx, options), compilation) = source;
            var resourceClassName = Path.GetFileNameWithoutExtension(resx.Path);

            var text = resx.GetText(spc.CancellationToken)?.ToString();
            if (string.IsNullOrEmpty(text))
                return;

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(text);

            options.GetOptions(resx).TryGetValue("build_metadata.AdditionalFiles.RelativePath", out var relativePath);
            options.GetOptions(resx).TryGetValue("build_property.RootNamespace", out var rootNamespace);

            var relativeNamespace = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '.').Replace(" ", "_");

            var namespaceName = string.IsNullOrEmpty(relativeNamespace) ? rootNamespace : $"{rootNamespace}.{relativeNamespace}";

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Resources;");
            sourceBuilder.AppendLine("using System.Globalization;");
            sourceBuilder.AppendLine("using System.Text;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {namespaceName}");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine($"    public static partial class {resourceClassName}");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine($"        private static readonly ResourceManager resourceManager = new ResourceManager(\"{namespaceName}.{resourceClassName}\", typeof({resourceClassName}).Assembly);");
            sourceBuilder.AppendLine("        public static ResourceManager ResourceManager => resourceManager;");
            sourceBuilder.AppendLine();

            foreach (XmlNode node in xmlDoc.SelectNodes("//data"))
            {
                var nameAttr = node.Attributes?["name"];
                var valueNode = node.SelectSingleNode("value");
                if (nameAttr is null || valueNode is null)
                    continue;

                var resourceName = nameAttr.Value;
                var resourceValue = valueNode.InnerText;

                if (InterpolationRegex.IsMatch(resourceValue))
                {
                    sourceBuilder.AppendLine($"        private static readonly CompositeFormat {resourceName}Format = CompositeFormat.Parse(ResourceManager.GetString(\"{resourceName}\", CultureInfo.CurrentUICulture)!);");
                    sourceBuilder.AppendLine($"        public static CompositeFormat {resourceName} => {resourceName}Format;");
                }
                else
                {
                    sourceBuilder.AppendLine($"        private static readonly string {resourceName}Value = ResourceManager.GetString(\"{resourceName}\", CultureInfo.CurrentUICulture)!;");
                    sourceBuilder.AppendLine($"        public static string {resourceName} => {resourceName}Value;");
                }

                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            spc.AddSource($"{resourceClassName}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        });
    }
}
