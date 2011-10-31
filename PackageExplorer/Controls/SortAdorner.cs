using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PackageExplorer
{
    public class SortAdorner : Adorner
    {
        private static readonly Geometry _DescGeometry = Geometry.Parse("M 0,0 L 10,0 L 5,5 Z");

        private static readonly Geometry _AscGeometry = Geometry.Parse("M 0,5 L 10,5 L 5,0 Z");

        public SortAdorner(UIElement element, ListSortDirection dir)
            : base(element)
        {
            Direction = dir;
        }

        public ListSortDirection Direction { get; private set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
            {
                return;
            }

            if (Direction == ListSortDirection.Descending)
            {
                drawingContext.PushTransform(
                    new TranslateTransform(AdornedElement.RenderSize.Width/2 - 5, 0));
            }
            else
            {
                drawingContext.PushTransform(
                    new TranslateTransform(AdornedElement.RenderSize.Width/2 - 5, AdornedElement.RenderSize.Height - 5));
            }

            drawingContext.DrawGeometry(
                SystemColors.ControlDarkBrush,
                null, /* pen */
                Direction == ListSortDirection.Ascending ? _AscGeometry : _DescGeometry);

            drawingContext.Pop();
        }
    }
}