using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PackageExplorer.Controls
{
    internal class EllipseDetails
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public Brush Fill { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }

    internal class EllipseData : ObservableCollection<EllipseDetails>
    {
        private static readonly double[] LeftCoordinates = new[] {
                20.1696, 2.86816, 5.03758e-006, 12.1203, 36.5459, 64.6723, 87.6176, 98.165, 92.9838, 47.2783
            };

        private static readonly double[] TopCoordinates = new[] {
                9.76358, 29.9581, 57.9341, 83.3163, 98.138, 96.8411, 81.2783, 54.414, 26.9938, 0.5
            };

        private static readonly int[] Opacities = new[] {
                0xE6, 0xCD, 0xB3, 0x9A, 0x80, 0x67, 0x4D, 0x34, 0x1A, 0xFF
            };

        private readonly SolidColorBrush IndicatorFill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#007ACC"));

        public EllipseData() : base()
        {
            var baseColor = IndicatorFill.Color;

            Enumerable.Range(0, LeftCoordinates.Length)
                .Select(i => new EllipseDetails
                {
                    Width = 21.835,
                    Height = 21.862,
                    Left = LeftCoordinates[i],
                    Top = TopCoordinates[i],
                    Fill = new SolidColorBrush(Color.FromArgb((byte)Opacities[i], baseColor.R, baseColor.G, baseColor.B))
                })
                .ToList()
                .ForEach(e => Add(e));
        }
    }

    /// <summary>
    /// Interaction logic for Spinner.xaml
    /// </summary>
    public partial class Spinner : UserControl
    {
        public Spinner()
        {
            InitializeComponent();
        }
    }

    internal sealed class CanvasScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var canvasWidthOrHeight = (double)parameter;
            var gridWidthOrHeight = (double)value;
            return gridWidthOrHeight / canvasWidthOrHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
