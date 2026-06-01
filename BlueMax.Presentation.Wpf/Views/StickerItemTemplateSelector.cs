using System.Windows.Controls;
using System.Windows;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views
{
    public class StickerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? LogoTemplate { get; set; }
        public DataTemplate? QrTemplate { get; set; }
        public DataTemplate? LineTemplate { get; set; }
        public DataTemplate? TextTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is StickerItem stickerItem)
            {
                var key = stickerItem.Key?.ToLowerInvariant() ?? "";
                return key switch
                {
                    "logo" => LogoTemplate,
                    "qr" => QrTemplate,
                    _ when key.StartsWith("line") => LineTemplate,
                    _ => TextTemplate,
                };
            }
            return base.SelectTemplate(item, container);
        }
    }
}
