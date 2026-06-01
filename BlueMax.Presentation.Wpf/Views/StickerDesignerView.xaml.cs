using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BlueMax.Presentation.Wpf.ViewModels;
using System.Windows.Input;
using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Shapes;
 
namespace BlueMax.Presentation.Wpf.Views;
 
public partial class StickerDesignerView : UserControl
{
    public StickerDesignerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    void OnLoaded(object? sender, System.Windows.RoutedEventArgs e)
    {
        StickerCanvas.SizeChanged += OnCanvasSizeChanged;
        SubscribeVm();
        RedrawGrid();
    }

    void OnUnloaded(object? sender, System.Windows.RoutedEventArgs e)
    {
        StickerCanvas.SizeChanged -= OnCanvasSizeChanged;
        UnsubscribeVm();
    }

    void OnCanvasSizeChanged(object? sender, System.Windows.SizeChangedEventArgs e)
    {
        RedrawGrid();
    }

    StickerDesignerViewModel? _vm;
    void SubscribeVm()
    {
        _vm = DataContext as StickerDesignerViewModel;
        if (_vm != null)
        {
            _vm.PropertyChanged += VmOnPropertyChanged;
        }
    }

    void UnsubscribeVm()
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= VmOnPropertyChanged;
            _vm = null;
        }
    }

    void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StickerDesignerViewModel.ShowGrid)
            or nameof(StickerDesignerViewModel.SnapStepMm)
            or nameof(StickerDesignerViewModel.CanvasWidthPx)
            or nameof(StickerDesignerViewModel.CanvasHeightPx)
            or nameof(StickerDesignerViewModel.PreviewScale))
        {
            RedrawGrid();
        }
    }

    void RedrawGrid()
    {
        if (GridOverlay == null) return;
        GridOverlay.Children.Clear();
        var vm = DataContext as StickerDesignerViewModel;
        if (vm == null || !vm.ShowGrid) return;

        var w = Math.Max(0.0, vm.CanvasWidthPx);
        var h = Math.Max(0.0, vm.CanvasHeightPx);
        if (w <= 0 || h <= 0) return;

        const double mmToPx = 1.0 / 0.264583;
        var stepMm = Math.Max(0.2, vm.SnapStepMm);
        var stepPx = stepMm * mmToPx;

        int index = 0;
        for (double x = 0; x <= w + 0.5; x += stepPx, index++)
        {
            var line = new Line
            {
                X1 = x, Y1 = 0,
                X2 = x, Y2 = h,
                Stroke = new SolidColorBrush(index % 5 == 0 ? Color.FromRgb(0xCC,0xCC,0xCC) : Color.FromRgb(0xE8,0xE8,0xE8)),
                StrokeThickness = index % 5 == 0 ? 1.0 : 0.5,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };
            GridOverlay.Children.Add(line);
        }

        index = 0;
        for (double y = 0; y <= h + 0.5; y += stepPx, index++)
        {
            var line = new Line
            {
                X1 = 0, Y1 = y,
                X2 = w, Y2 = y,
                Stroke = new SolidColorBrush(index % 5 == 0 ? Color.FromRgb(0xCC,0xCC,0xCC) : Color.FromRgb(0xE8,0xE8,0xE8)),
                StrokeThickness = index % 5 == 0 ? 1.0 : 0.5,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };
            GridOverlay.Children.Add(line);
        }
    }

    void StickerThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not StickerItem item)
            return;

        // Clamp within canvas bounds
        var vm = DataContext as StickerDesignerViewModel;
        var scale = vm?.PreviewScale ?? 1.0;
        var canvasW = Math.Max(0.0, vm?.CanvasWidthPx ?? StickerCanvas.ActualWidth);
        var canvasH = Math.Max(0.0, vm?.CanvasHeightPx ?? StickerCanvas.ActualHeight);
        
        // Use item's current clamped width/height for calculating max X/Y
        var itemW = item.Width; // Use item's width, which should be clamped
        var itemH = item.Height; // Use item's height, which should be clamped
        
        // subtract element size to keep it fully inside
        var maxX = Math.Max(0, canvasW - itemW);
        var maxY = Math.Max(0, canvasH - itemH);
        
        // Calculate new position
        var deltaX = e.HorizontalChange / Math.Max(0.01, scale);
        var deltaY = e.VerticalChange / Math.Max(0.01, scale);
        var proposedX = item.X + deltaX;
        var proposedY = item.Y + deltaY;
        
        // Apply snapping if enabled
        if (vm != null && vm.SnapToGrid)
        {
             const double MmToPx = 1.0 / 0.264583;
             var stepPx = Math.Max(0.1, vm.SnapStepMm) * MmToPx;
             proposedX = Math.Round(proposedX / stepPx) * stepPx;
             proposedY = Math.Round(proposedY / stepPx) * stepPx;
        }
        
        // Clamp
        proposedX = Math.Max(0, Math.Min(proposedX, maxX));
        proposedY = Math.Max(0, Math.Min(proposedY, maxY));
        
        item.X = proposedX;
        item.Y = proposedY;
    }

    void StickerThumb_OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not StickerItem item)
            return;

        var canvasW = StickerCanvas.ActualWidth;
        var canvasH = StickerCanvas.ActualHeight;

        // Clamp new width and height within canvas bounds
        var newWidth = Math.Min(e.NewSize.Width, canvasW > 0 ? canvasW : e.NewSize.Width);
        var newHeight = Math.Min(e.NewSize.Height, canvasH > 0 ? canvasH : e.NewSize.Height);

        item.Width = newWidth;
        item.Height = newHeight;

        // Clamp position if it goes out of bounds due to size change
        if (canvasW > 0 && canvasH > 0)
        {
             var maxX = Math.Max(0, canvasW - item.Width);
             var maxY = Math.Max(0, canvasH - item.Height);
             
             if (item.X > maxX) item.X = maxX;
             if (item.Y > maxY) item.Y = maxY;
        }
    }

    void StickerResizeThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not StickerItem item)
            return;

        var vm = DataContext as StickerDesignerViewModel;
        var scale = vm?.PreviewScale ?? 1.0;
        var canvasW = Math.Max(0.0, vm?.CanvasWidthPx ?? StickerCanvas.ActualWidth);
        var canvasH = Math.Max(0.0, vm?.CanvasHeightPx ?? StickerCanvas.ActualHeight);

        var deltaX = e.HorizontalChange / Math.Max(0.01, scale);
        var deltaY = e.VerticalChange / Math.Max(0.01, scale);
        var proposedW = item.Width + deltaX;
        var proposedH = item.Height + deltaY;

        if (vm != null && vm.SnapToGrid)
        {
            const double MmToPx = 1.0 / 0.264583;
            var stepPx = Math.Max(0.1, vm.SnapStepMm) * MmToPx;
            proposedW = Math.Round(proposedW / stepPx) * stepPx;
            proposedH = Math.Round(proposedH / stepPx) * stepPx;
        }

        var minW = 10.0;
        var minH = 10.0;
        proposedW = Math.Max(minW, Math.Min(proposedW, canvasW - item.X));
        proposedH = Math.Max(minH, Math.Min(proposedH, canvasH - item.Y));

        item.Width = proposedW;
        item.Height = proposedH;
    }

    void StickerThumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not StickerItem item)
            return;
        if (DataContext is StickerDesignerViewModel vm)
        {
            vm.SelectedStickerItem = item;
            e.Handled = true;
        }
    }
}
