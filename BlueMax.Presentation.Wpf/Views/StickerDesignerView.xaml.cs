using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BlueMax.Presentation.Wpf.ViewModels;
using BlueMax.Presentation.Wpf.Services;
using System.Windows.Input;
using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows;
 
namespace BlueMax.Presentation.Wpf.Views;
 
public partial class StickerDesignerView : UserControl
{
    const double MmToPx = 1.0 / 0.264583;
    DispatcherTimer? _gridRedrawTimer;

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
        FitZoom();
        Focusable = true;
        Focus();
        KeyDown += OnDesignerKeyDown;
        PreviewScroller.SizeChanged += (_, __) => FitZoom();
    }

    void OnUnloaded(object? sender, System.Windows.RoutedEventArgs e)
    {
        StickerCanvas.SizeChanged -= OnCanvasSizeChanged;
        UnsubscribeVm();
    }

    void OnCanvasSizeChanged(object? sender, System.Windows.SizeChangedEventArgs e)
    {
        RedrawGrid();
        FitZoom();
    }

    void FitZoom()
    {
        var vm = DataContext as StickerDesignerViewModel;
        if (vm == null || vm.CanvasWidthPx <= 0 || vm.CanvasHeightPx <= 0) return;
        if (PreviewScroller.ActualWidth <= 0 || PreviewScroller.ActualHeight <= 0) return;

        var availW = Math.Max(50, PreviewScroller.ActualWidth - 48);
        var availH = Math.Max(50, PreviewScroller.ActualHeight - 48);
        var fit = Math.Min(availW / vm.CanvasWidthPx, availH / vm.CanvasHeightPx);
        vm.Zoom = Math.Max(0.5, Math.Min(4.0, fit));
    }

    void OnDesignerKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is not StickerDesignerViewModel vm) return;
        var item = vm.SelectedStickerItem;
        if (item == null) return;

        var step = 0.25 * MmToPx;
        var deltaX = 0.0;
        var deltaY = 0.0;
        switch (e.Key)
        {
            case System.Windows.Input.Key.Left: deltaX = -step; break;
            case System.Windows.Input.Key.Right: deltaX = step; break;
            case System.Windows.Input.Key.Up: deltaY = -step; break;
            case System.Windows.Input.Key.Down: deltaY = step; break;
            default: return;
        }

        var maxX = Math.Max(0, vm.CanvasWidthPx - item.Width);
        var maxY = Math.Max(0, vm.CanvasHeightPx - item.Height);
        item.X = Math.Max(0, Math.Min(item.X + deltaX, maxX));
        item.Y = Math.Max(0, Math.Min(item.Y + deltaY, maxY));
        e.Handled = true;
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
        if (_gridRedrawTimer == null)
        {
            _gridRedrawTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _gridRedrawTimer.Tick += (_, __) =>
            {
                _gridRedrawTimer.Stop();
                DrawGrid();
            };
        }
        _gridRedrawTimer.Stop();
        _gridRedrawTimer.Start();
    }

    void DrawGrid()
    {
        if (GridOverlay == null) return;
        GridOverlay.Children.Clear();
        var vm = DataContext as StickerDesignerViewModel;
        if (vm == null || !vm.ShowGrid) return;

        var w = Math.Max(0.0, vm.CanvasWidthPx);
        var h = Math.Max(0.0, vm.CanvasHeightPx);
        if (w <= 0 || h <= 0) return;

        var stepMm = Math.Max(0.2, vm.SnapStepMm);
        var stepPx = stepMm * MmToPx;

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

    void CustomTemplateItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not StickerDesignerViewModel vm) return;
        if (sender is ListBoxItem { DataContext: StickerTemplateInfo template })
        {
            vm.ApplyCustomTemplateCommand.Execute(template);
            e.Handled = true;
        }
    }

    void StickerThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not StickerItem item)
            return;

        // Clamp within canvas bounds
        var vm = DataContext as StickerDesignerViewModel;
        var canvasW = Math.Max(0.0, vm?.CanvasWidthPx ?? StickerCanvas.ActualWidth);
        var canvasH = Math.Max(0.0, vm?.CanvasHeightPx ?? StickerCanvas.ActualHeight);
        
        // Use item's current clamped width/height for calculating max X/Y
        var itemW = item.Width; // Use item's width, which should be clamped
        var itemH = item.Height; // Use item's height, which should be clamped
        
        // subtract element size to keep it fully inside
        var maxX = Math.Max(0, canvasW - itemW);
        var maxY = Math.Max(0, canvasH - itemH);
        
        // Calculate new position (DragDelta is already in the canvas logical coordinate space)
        var deltaX = e.HorizontalChange;
        var deltaY = e.VerticalChange;
        if (thumb.FlowDirection == FlowDirection.RightToLeft)
            deltaX = -deltaX;
        var proposedX = item.X + deltaX;
        var proposedY = item.Y + deltaY;
        
        // Apply snapping if enabled
        if (vm != null && vm.SnapToGrid)
        {
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

    void StickerResizeThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.Tag is not StickerItem item)
            return;

        var vm = DataContext as StickerDesignerViewModel;
        var canvasW = Math.Max(0.0, vm?.CanvasWidthPx ?? StickerCanvas.ActualWidth);
        var canvasH = Math.Max(0.0, vm?.CanvasHeightPx ?? StickerCanvas.ActualHeight);

        // DragDelta is already in the canvas logical coordinate space
        var deltaX = e.HorizontalChange;
        var deltaY = e.VerticalChange;
        if (thumb.FlowDirection == FlowDirection.RightToLeft)
            deltaX = -deltaX;
        var proposedW = item.Width + deltaX;
        var proposedH = item.Height + deltaY;

        if (vm != null && vm.SnapToGrid)
        {
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

    void StickerThumb_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb && thumb.Tag is StickerItem item &&
            DataContext is StickerDesignerViewModel vm)
        {
            vm.SelectedStickerItem = item;
        }
    }
}
