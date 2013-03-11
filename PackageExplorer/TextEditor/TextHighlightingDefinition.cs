using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PackageExplorer
{
    internal class TextHighlightingDefinition : IHighlightingDefinition
    {
        public static readonly TextHighlightingDefinition Instance = new TextHighlightingDefinition();
        private static readonly HighlightingRuleSet _emptyRuleSet = new HighlightingRuleSet();

        private TextHighlightingDefinition()
        {
        }

        public HighlightingColor GetNamedColor(string name)
        {
            return null;
        }

        public HighlightingRuleSet GetNamedRuleSet(string name)
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
    }
}
