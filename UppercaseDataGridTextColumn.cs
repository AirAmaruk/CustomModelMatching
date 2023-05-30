using System.Globalization;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CustomModelMatching
{
    public class UppercaseDataGridTextColumn : DataGridTextColumn
    {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var textBlock = (TextBlock)base.GenerateElement(cell, dataItem);
            textBlock.SetBinding(TextBlock.TextProperty, new Binding(((Binding)this.Binding).Path.Path)
            {
                Converter = new UppercaseConverter(),
                Mode = BindingMode.OneWay
            });
            return textBlock;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var textBox = (TextBox)base.GenerateEditingElement(cell, dataItem);
            textBox.CharacterCasing = CharacterCasing.Upper;
            return textBox;
        }
    }

    public class UppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text.ToUpper();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
