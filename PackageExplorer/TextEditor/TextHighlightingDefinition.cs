using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PackageExplorer
{
    internal class TextHighlightingDefinition : IHighlightingDefinition
    {
        public static readonly TextHighlightingDefinition Instance = new TextHighlightingDefinition();
        private static readonly HighlightingRuleSet _emptyRuleSet = new HighlightingRuleSet();
        private static readonly Dictionary<string, string> _emptyProperties = new Dictionary<string, string>();

        private TextHighlightingDefinition()
        {
        }

        public HighlightingColor? GetNamedColor(string name)
        {
            return null;
        }

        public HighlightingRuleSet? GetNamedRuleSet(string name)
        {
            return null;
        }

        public HighlightingRuleSet MainRuleSet
        {
            get
            {
                return _emptyRuleSet;
            }
        }

        public string Name
        {
            get { return "Plain Text"; }
        }

        public IEnumerable<HighlightingColor> NamedHighlightingColors
        {
            get { return Enumerable.Empty<HighlightingColor>(); }
        }

        public IDictionary<string, string> Properties => _emptyProperties;
    }
}
