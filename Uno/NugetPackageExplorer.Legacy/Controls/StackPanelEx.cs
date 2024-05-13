using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Controls
{
    public partial class StackPanelEx : StackPanel
    {
        // workaround for: https://github.com/microsoft/microsoft-ui-xaml/issues/916
        // modified from: https://github.com/unoplatform/uno/blob/a7158b41df67f1e02dd48ffe395ea1d2b07e96e4/src/Uno.UI/UI/Xaml/Controls/StackPanel/StackPanel.Layout.cs

        protected override Size MeasureOverride(Size availableSize)
        {
            var borderAndPaddingSize = BorderAndPaddingSize;
            //availableSize = availableSize.Subtract(borderAndPaddingSize);
            availableSize = new Size(
                availableSize.Width - borderAndPaddingSize.Width,
                availableSize.Height - borderAndPaddingSize.Height);

            var desiredSize = default(Size);
            var isHorizontal = Orientation == Orientation.Horizontal;
            var slotSize = availableSize;

            if (isHorizontal)
            {
                slotSize.Width = float.PositiveInfinity;
            }
            else
            {
                slotSize.Height = float.PositiveInfinity;
            }

            // Shadow variables for evaluation performance
            var spacing = Spacing;
            var count = Children.Count;

            var isAnyPreviousViewVisible = false;
            for (int i = 0; i < count; i++)
            {
                var view = Children[i];

                view.Measure(slotSize);
                var measuredSize = view.DesiredSize;

                // only insert spacing between visible views
                //var addSpacing = i != count - 1;
                var isVisible = view.Visibility == Visibility.Visible;
                var addSpacing = isAnyPreviousViewVisible && isVisible;
                isAnyPreviousViewVisible |= isVisible;

                if (isHorizontal)
                {
                    desiredSize.Width += measuredSize.Width;
                    desiredSize.Height = Math.Max(desiredSize.Height, measuredSize.Height);

                    if (addSpacing)
                    {
                        desiredSize.Width += spacing;
                    }
                }
                else
                {
                    desiredSize.Width = Math.Max(desiredSize.Width, measuredSize.Width);
                    desiredSize.Height += measuredSize.Height;

                    if (addSpacing)
                    {
                        desiredSize.Height += spacing;
                    }
                }
            }

            //return desiredSize.Add(borderAndPaddingSize);
            return new Size(
                desiredSize.Width + borderAndPaddingSize.Width,
                desiredSize.Height + borderAndPaddingSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var borderAndPaddingSize = BorderAndPaddingSize;
            //arrangeSize = arrangeSize.Subtract(borderAndPaddingSize);
            arrangeSize = new Size(
                arrangeSize.Width - borderAndPaddingSize.Width,
                arrangeSize.Height - borderAndPaddingSize.Height);

            var childRectangle = new Rect(BorderThickness.Left + Padding.Left, BorderThickness.Top + Padding.Top, arrangeSize.Width, arrangeSize.Height);

            var isHorizontal = Orientation == Orientation.Horizontal;
            var previousChildSize = 0.0;

            /*if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                this.Log().Debug($"StackPanel/{Name}: Arranging {Children.Count} children.");
            }*/

            // Shadow variables for evaluation performance
            var spacing = Spacing;
            var count = Children.Count;

            /*var snapPoints = (_snapPoints ??= new List<float>(count)) as List<float>;

            var snapPointsChanged = snapPoints.Count != count;

            if(snapPoints.Capacity < count)
            {
                snapPoints.Capacity = count;
            }

            while(snapPoints.Count < count)
            {
                snapPoints.Add(default);
            }

            while(snapPoints.Count > count)
            {
                snapPoints.RemoveAt(count);
            }*/

            var isAnyPreviousViewVisible = false;
            for (var i = 0; i < count; i++)
            {
                var view = Children[i];
                var desiredChildSize = view.DesiredSize;

                // only insert spacing between visible views
                //var addSpacing = i != 0;
                var isVisible = view.Visibility == Visibility.Visible;
                var addSpacing = isAnyPreviousViewVisible && isVisible;
                isAnyPreviousViewVisible |= isVisible;

                if (isHorizontal)
                {
                    childRectangle.X += previousChildSize;

                    if (addSpacing)
                    {
                        childRectangle.X += spacing;
                    }

                    previousChildSize = desiredChildSize.Width;
                    childRectangle.Width = desiredChildSize.Width;
                    childRectangle.Height = Math.Max(arrangeSize.Height, desiredChildSize.Height);

                    /*var snapPoint = (float)childRectangle.Right;
                    snapPointsChanged |= snapPoints[i] == snapPoint;
                    snapPoints[i] = snapPoint;*/
                }
                else
                {
                    childRectangle.Y += previousChildSize;

                    if (addSpacing)
                    {
                        childRectangle.Y += spacing;
                    }

                    previousChildSize = desiredChildSize.Height;
                    childRectangle.Height = desiredChildSize.Height;
                    childRectangle.Width = Math.Max(arrangeSize.Width, desiredChildSize.Width);

                    /*var snapPoint = (float)childRectangle.Bottom;
                    snapPointsChanged |= snapPoints[i] == snapPoint;
                    snapPoints[i] = snapPoint;*/
                }

                var adjustedRectangle = childRectangle;

                view.Arrange(adjustedRectangle);
            }

            //var finalSizeWithBorderAndPadding = arrangeSize.Add(borderAndPaddingSize);
            var finalSizeWithBorderAndPadding = new Size(
                arrangeSize.Width + borderAndPaddingSize.Width,
                arrangeSize.Height + borderAndPaddingSize.Height);

            /*if(snapPointsChanged)
            {
                if(isHorizontal)
                {
                    HorizontalSnapPointsChanged?.Invoke(this, this);
                }
                else
                {
                    VerticalSnapPointsChanged?.Invoke(this, this);
                }
            }*/

            return finalSizeWithBorderAndPadding;
        }

        private Size BorderAndPaddingSize
        {
            get
            {
                var border = BorderThickness;
                var padding = Padding;
                var width = border.Left + border.Right + padding.Left + padding.Right;
                var height = border.Top + border.Bottom + padding.Top + padding.Bottom;

                return new Size(width, height);
            }
        }
    }
}
