using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PackageExplorer {
    public class BorderGapMaskConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            Type typeFromHandle = typeof(double);
            if (parameter == null || values == null || values.Length != 3 || values[0] == null || values[1] == null || values[2] == null || !typeFromHandle.IsAssignableFrom(values[0].GetType()) || !typeFromHandle.IsAssignableFrom(values[1].GetType()) || !typeFromHandle.IsAssignableFrom(values[2].GetType())) {
                return DependencyProperty.UnsetValue;
            }

            Type type = parameter.GetType();
            if (!typeFromHandle.IsAssignableFrom(type) && !typeof(string).IsAssignableFrom(type)) {
                return DependencyProperty.UnsetValue;
            }

            double headerWidth = (double)values[0];
            double borderWidth = (double)values[1];
            double borderHeight = (double)values[2];
            if (borderWidth == 0.0 || borderHeight == 0.0) {
                return null;
            }
            Grid grid = new Grid();
            grid.Width = borderWidth;
            grid.Height = borderHeight;
            ColumnDefinition columnDefinition = new ColumnDefinition();
            ColumnDefinition columnDefinition2 = new ColumnDefinition();
            ColumnDefinition columnDefinition3 = new ColumnDefinition();
            columnDefinition.Width = new GridLength(1.0, GridUnitType.Star);
            columnDefinition2.Width = new GridLength(headerWidth);
            columnDefinition3.Width = new GridLength(1.0, GridUnitType.Star);
            grid.ColumnDefinitions.Add(columnDefinition);
            grid.ColumnDefinitions.Add(columnDefinition2);
            grid.ColumnDefinitions.Add(columnDefinition3);
            RowDefinition rowDefinition = new RowDefinition();
            RowDefinition rowDefinition2 = new RowDefinition();
            rowDefinition.Height = new GridLength(borderHeight / 2.0);
            rowDefinition2.Height = new GridLength(1.0, GridUnitType.Star);
            grid.RowDefinitions.Add(rowDefinition);
            grid.RowDefinitions.Add(rowDefinition2);
            Rectangle rectangle = new Rectangle();
            Rectangle rectangle2 = new Rectangle();
            Rectangle rectangle3 = new Rectangle();
            rectangle.Fill = Brushes.Black;
            rectangle2.Fill = Brushes.Black;
            rectangle3.Fill = Brushes.Black;
            Grid.SetRowSpan(rectangle, 2);
            Grid.SetRow(rectangle, 0);
            Grid.SetColumn(rectangle, 0);
            Grid.SetRow(rectangle2, 1);
            Grid.SetColumn(rectangle2, 1);
            Grid.SetRowSpan(rectangle3, 2);
            Grid.SetRow(rectangle3, 0);
            Grid.SetColumn(rectangle3, 2);
            grid.Children.Add(rectangle);
            grid.Children.Add(rectangle2);
            grid.Children.Add(rectangle3);
            return new VisualBrush(grid);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return new object[] { 
                Binding.DoNothing
            };
        }
    }
}
