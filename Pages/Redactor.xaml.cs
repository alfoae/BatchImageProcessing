using ImageProcessing.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ImageService;

namespace ImageProcessing
{
    public partial class Redactor : Page
    {
        public Redactor()
        {
            InitializeComponent();

            WatermarksCanvas.MouseDown += WatermarksCanvas_MouseDown;

            SlideLabel.Value = 52;
            SlideLabel.Value = 51;
            CropOverlay.Width = 100;
            CropOverlay.Height = 100;
            CropTranslate.X = 0;
            CropTranslate.Y = 0;

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible && CurrentCrop != Int32Rect.Empty)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        DisplayImage.UpdateLayout();
                        UpdateCropOverlayFromCurrent();
                        RenderWatermarks();
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }
            };

            Initialize(DisplayImage, ThumbnailListBox);
            RefreshFileList(ThumbnailListBox);



            KeyDown += MainWindow_KeyDown;
            WatermarkAlignmentChanged += () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RenderWatermarks();
                }), System.Windows.Threading.DispatcherPriority.Render);
            };

            CurrentImageChanged += (bitmap) =>
			{
				DisplayImage.UpdateLayout();
				UpdateCropOverlayFromCurrent();
                RenderWatermarks();
            };

            CurrentCropChanged += (crop) =>
            {
                if (!IsVisible) return;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DisplayImage.UpdateLayout();
                    UpdateCropOverlayFromCurrent();
                    RenderWatermarks();
                }), System.Windows.Threading.DispatcherPriority.Render);
            };
        }

        private double zoomcounter = 100;
        private double SliderValue = 51;
        private bool _isDragging;
        private Point _dragStart;
        private bool _isResizing;
        private Point _resizeStart;
        private double _startWidth;
        private double _startHeight;
        private string _currentResizeDirection = "";
        private double _startCropX;
        private double _startCropY;
        private bool _isDraggingWatermark = false;
        private Point _startMousePosition;
        private double _startWatermarkX;
        private double _startWatermarkY;

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ZoomLabel == null) return;
            zoomcounter = RedactorPageService.ZoomSlider_ValueChanged(sender, e);
            CenteredImage(sender);
            ZoomLabel.Text = $"{Math.Round(zoomcounter, 1)}%";
            ApplyZoom(ZoomContainer, zoomcounter);
            SliderValue = SlideLabel.Value;
        }

        private void ZoomPlusMinus(object sender, RoutedEventArgs e)
        {
            SlideLabel.Value = RedactorPageService.ZoomButtons(sender, SliderValue, e);
        }

        private void DisplayImage_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomPlusMinus(null, e);

                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {

                if (e.Delta > 0)
                {
                    for (byte i = 0; i < 10; i++)
                        MyScrollViewer.LineLeft();
                }
                else
                {
                    for (byte i = 0; i < 10; i++)
                        MyScrollViewer.LineRight();
                }

                e.Handled = true;
            }
            else
            {
                if (e.Delta > 0)
                    MyScrollViewer.LineUp();
                else
                    MyScrollViewer.LineDown();
                e.Handled = false;
            }
        }

        private void CenteredImage(object sender)
        {
            if (zoomcounter <= 51)
            {
                MainTranslateTransform.X = 0;
                MainTranslateTransform.Y = 0;

                MyScrollViewer.ScrollToHorizontalOffset(0);
                MyScrollViewer.ScrollToVerticalOffset(0);

                CenterImage(DisplayImage);
            }
        }

        private void MyScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (e.Delta > 0)
                {
                    MyScrollViewer.LineLeft();
                }
                else
                {
                    MyScrollViewer.LineRight();
                }

                e.Handled = true;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                Navigate(false);
                SlideLabel.Value = 51;
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                Navigate(true);
                SlideLabel.Value = 51;
                e.Handled = true;
            }
        }

        private void ThumbnailListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThumbnailListBox.SelectedIndex != -1)
            {
                int selectedIndex = ThumbnailListBox.SelectedIndex;
                LoadImageByIndex(selectedIndex);
            }
        }

        private void CropOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(ZoomContainer);

            if (sender is UIElement element)
            {
                element.CaptureMouse();
            }

            e.Handled = true;
        }

        private void CropOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point current = e.GetPosition(ZoomContainer);

            double dx = current.X - _dragStart.X;
            double dy = current.Y - _dragStart.Y;

            if (ImageService.IsCurrentAlignmentEnabled)
            {
                string alignType = ImageService.CurrentAlignmentType;
                if (alignType == "Центру")
                {
                    dx = 0;
                    dy = 0;
                }
                else if (alignType == "Висоті")
                {
                    dx = 0;
                }
                else if (alignType == "Ширині")
                {
                    dy = 0;
                }
            }

            CropTranslate.X += dx;
            CropTranslate.Y += dy;

            _dragStart = current;
        }

        private void CropOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;

            if (sender is UIElement element)
            {
                element.ReleaseMouseCapture();
            }

			CurrentCrop = GetAbsoluteCropRect();
		}

        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                _isResizing = true;
                _currentResizeDirection = handle.Tag.ToString();
                handle.CaptureMouse();

                _resizeStart = e.GetPosition(ZoomContainer);
                _startWidth = CropOverlay.Width;
                _startHeight = CropOverlay.Height;
                _startCropX = CropTranslate.X;
                _startCropY = CropTranslate.Y;

                e.Handled = true;
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizing) return;

            Point current = e.GetPosition(ZoomContainer);
            double dx = current.X - _resizeStart.X;
            double dy = current.Y - _resizeStart.Y;

            double newWidth = _startWidth;
            double newHeight = _startHeight;
            double newX = _startCropX;
            double newY = _startCropY;

            bool alignmentEnabled = IsCurrentAlignmentEnabled;
            string alignType = CurrentAlignmentType;

            if (_currentResizeDirection.Contains("Right"))
            {
                if (alignmentEnabled && (alignType == "Центру" || alignType == "Висоті"))
                {
                    newWidth = _startWidth + 2 * dx;
                    newX = _startCropX - dx;
                }
                else
                {
                    newWidth = _startWidth + dx;
                }
            }
            else if (_currentResizeDirection.Contains("Left"))
            {
                if (alignmentEnabled && (alignType == "Центру" || alignType == "Висоті"))
                {
                    newWidth = _startWidth - 2 * dx;
                    newX = _startCropX + dx;
                }
                else
                {
                    newWidth = _startWidth - dx;
                    newX = _startCropX + dx;
                }
            }

            if (_currentResizeDirection.Contains("Bottom"))
            {
                if (alignmentEnabled && (alignType == "Центру" || alignType == "Ширині"))
                {
                    newHeight = _startHeight + 2 * dy;
                    newY = _startCropY - dy;
                }
                else
                {
                    newHeight = _startHeight + dy;
                }
            }
            else if (_currentResizeDirection.Contains("Top"))
            {
                if (alignmentEnabled && (alignType == "Центру" || alignType == "Ширині"))
                {
                    newHeight = _startHeight - 2 * dy;
                    newY = _startCropY + dy;
                }
                else
                {
                    newHeight = _startHeight - dy;
                    newY = _startCropY + dy;
                }
            }

            if (newWidth >= 50)
            {
                CropOverlay.Width = newWidth;
                CropTranslate.X = newX;
            }
            if (newHeight >= 50)
            {
                CropOverlay.Height = newHeight;
                CropTranslate.Y = newY;
            }
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                if (sender is Rectangle handle)
                {
                    handle.ReleaseMouseCapture();
                }
            }

            CurrentCrop = GetAbsoluteCropRect();
        }

        public Int32Rect GetAbsoluteCropRect()
        {
            if (DisplayImage == null || CropOverlay == null || CurrentBitmap == null)
                return Int32Rect.Empty;

            try
            {
                Point topLeft = CropOverlay.TransformToVisual(DisplayImage).Transform(new Point(0, 0));

                double scaleX = DisplayImage.ActualWidth / CurrentBitmap.PixelWidth;
                double scaleY = DisplayImage.ActualHeight / CurrentBitmap.PixelHeight;
                double uniformScale = Math.Min(scaleX, scaleY);

                double screenCenterX = DisplayImage.ActualWidth / 2.0;
                double screenCenterY = DisplayImage.ActualHeight / 2.0;

                double screenOverlayCenterX = topLeft.X + CropOverlay.ActualWidth / 2.0;
                double screenOverlayCenterY = topLeft.Y + CropOverlay.ActualHeight / 2.0;

                double screenOffsetX = screenOverlayCenterX - screenCenterX;
                double screenOffsetY = screenOverlayCenterY - screenCenterY;

                int userX = (int)Math.Round(screenOffsetX / uniformScale);
                int userY = (int)Math.Round(screenOffsetY / uniformScale);

                int realWidth = (int)Math.Round(CropOverlay.ActualWidth / uniformScale);
                int realHeight = (int)Math.Round(CropOverlay.ActualHeight / uniformScale);

                return new Int32Rect(userX, userY, realWidth, realHeight);
            }
            catch
            {
                return Int32Rect.Empty;
            }
        }

        private void UpdateCropOverlayFromCurrent()
        {
            Rect rect = GetScreenRect(
                CurrentCrop.X,
                CurrentCrop.Y,
                CurrentCrop.Width,
                CurrentCrop.Height
            );

            CropOverlay.Width = rect.Width;
            CropOverlay.Height = rect.Height;

            Canvas.SetLeft(CropOverlay, rect.X);
            Canvas.SetTop(CropOverlay, rect.Y);

            CropTranslate.X = 0;
            CropTranslate.Y = 0;
        }

        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
        //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK

        private Border? _selectedWatermarkBorder = null;

        private void RenderWatermarks()
        {
            WatermarksCanvas.Children.Clear();
            if (CurrentBitmap == null) return;

            double scaleX = DisplayImage.ActualWidth / CurrentBitmap.PixelWidth;
            double scaleY = DisplayImage.ActualHeight / CurrentBitmap.PixelHeight;
            double uniformScale = Math.Min(scaleX, scaleY);
            double imgScreenX = (DisplayImage.ActualWidth / 2.0) - (CurrentBitmap.PixelWidth * uniformScale / 2.0);
            double imgScreenY = (DisplayImage.ActualHeight / 2.0) - (CurrentBitmap.PixelHeight * uniformScale / 2.0);

            VisualBrush contrastBrush = new VisualBrush();
            Grid brushGrid = new Grid();
            brushGrid.Children.Add(new Rectangle { Stroke = Brushes.White, StrokeThickness = 1.5 });
            brushGrid.Children.Add(new Rectangle { Stroke = Brushes.Black, StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection() { 4, 4 } });
            contrastBrush.Visual = brushGrid;

            foreach (var layer in GetCurrentPhotoLayers())
            {
                Border wmBorder = new Border
                {
                    Width = layer.Width * uniformScale,
                    Height = layer.Height * uniformScale,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1.5),
                    Cursor = Cursors.SizeAll,
                    Tag = layer,
                    Opacity = layer.Opacity
                };

                Grid wmGrid = new Grid();
                wmGrid.Children.Add(new Image
                {
                    Source = new BitmapImage(new Uri(layer.ImagePath, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false
                });

                Rectangle moveRect = new Rectangle { Fill = Brushes.Transparent, Cursor = Cursors.SizeAll };
                wmGrid.Children.Add(moveRect);
                wmBorder.Child = wmGrid;

                wmGrid.Children.Add(new Rectangle { Width = 10, HorizontalAlignment = HorizontalAlignment.Left, Fill = Brushes.Transparent, Cursor = Cursors.SizeWE, Tag = "Left" });
                wmGrid.Children.Add(new Rectangle { Width = 10, HorizontalAlignment = HorizontalAlignment.Right, Fill = Brushes.Transparent, Cursor = Cursors.SizeWE, Tag = "Right" });
                wmGrid.Children.Add(new Rectangle { Height = 10, VerticalAlignment = VerticalAlignment.Top, Fill = Brushes.Transparent, Cursor = Cursors.SizeNS, Tag = "Top" });
                wmGrid.Children.Add(new Rectangle { Height = 10, VerticalAlignment = VerticalAlignment.Bottom, Fill = Brushes.Transparent, Cursor = Cursors.SizeNS, Tag = "Bottom" });
                wmGrid.Children.Add(new Rectangle { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Fill = Brushes.Transparent, Cursor = Cursors.SizeNWSE, Tag = "TopLeft" });
                wmGrid.Children.Add(new Rectangle { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Fill = Brushes.Transparent, Cursor = Cursors.SizeNWSE, Tag = "BottomRight" });
                wmGrid.Children.Add(new Rectangle { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Fill = Brushes.Transparent, Cursor = Cursors.SizeNESW, Tag = "TopRight" });
                wmGrid.Children.Add(new Rectangle { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Fill = Brushes.Transparent, Cursor = Cursors.SizeNESW, Tag = "BottomLeft" });

                Rect rect = GetScreenRect(
                    layer.X,
                    layer.Y,
                    layer.Width,
                    layer.Height
                );

                wmBorder.Width = rect.Width;
                wmBorder.Height = rect.Height;

                Canvas.SetLeft(wmBorder, rect.X);
                Canvas.SetTop(wmBorder, rect.Y);

                bool isDragging = false, isResizing = false;
                string currentHandleTag = "";
                Point startMouse = new Point(), wmStartPos = new Point();
                double startW = 0, startH = 0;

                wmBorder.MouseLeftButtonDown += (s, e) =>
                {
                    if (_selectedWatermarkBorder != null && _selectedWatermarkBorder != wmBorder)
                    {
                        _selectedWatermarkBorder.BorderBrush = Brushes.Transparent;
                    }

                    _selectedWatermarkBorder = wmBorder;
                    wmBorder.BorderBrush = contrastBrush;

                    if (e.OriginalSource == moveRect)
                    {
                        isDragging = true;
                        startMouse = e.GetPosition(WatermarksCanvas);
                        wmStartPos = new Point(layer.X, layer.Y);
                        wmBorder.CaptureMouse();
                        e.Handled = true;
                    }
                    else if (e.OriginalSource is Rectangle r && r.Tag != null)
                    {
                        isResizing = true;
                        currentHandleTag = r.Tag.ToString();
                        startMouse = e.GetPosition(WatermarksCanvas);
                        wmStartPos = new Point(layer.X, layer.Y);
                        startW = layer.Width;
                        startH = layer.Height;
                        wmBorder.CaptureMouse();
                        e.Handled = true;
                    }
                };

                wmBorder.MouseMove += (s, e) =>
                {
                    if (!wmBorder.IsMouseCaptured) return;
                    Point currMouse = e.GetPosition(WatermarksCanvas);
                    double deltaX = (currMouse.X - startMouse.X) / uniformScale;
                    double deltaY = (currMouse.Y - startMouse.Y) / uniformScale;

                    if (isDragging)
                    {
                        if (IsWatermarkAlignmentEnabled)
                        {
                            string alignment = SelectedWatermark?.SelectedAlignment ?? "Центру";
                            if (alignment != "Висоті" && alignment != "Ширині")
                            {
                                return;
                            }
                        }

                        layer.X = wmStartPos.X + deltaX;
                        layer.Y = wmStartPos.Y + deltaY;

                        ApplyWatermarkAlignment(layer, isDragging: true, startX: wmStartPos.X, startY: wmStartPos.Y);

                        if (IsSameIdAlignmentOn)
                        {
                            SyncWatermarkPositions(layer);
                        }

                        foreach (UIElement child in WatermarksCanvas.Children)
                        {
                            if (child is Border b && b.Tag is WatermarkLayer l)
                            {
                                Rect r = GetScreenRect(l.X, l.Y, l.Width, l.Height);
                                b.Width = r.Width;
                                b.Height = r.Height;
                                Canvas.SetLeft(b, r.X);
                                Canvas.SetTop(b, r.Y);
                            }
                        }
                    }
                    else if (isResizing)
                    {
                        switch (currentHandleTag)
                        {
                            case "Left":
                                layer.Width = Math.Max(10, startW - deltaX);
                                layer.X = wmStartPos.X + (startW - layer.Width) / 2.0;
                                break;
                            case "Right":
                                layer.Width = Math.Max(10, startW + deltaX);
                                layer.X = wmStartPos.X + (layer.Width - startW) / 2.0;
                                break;
                            case "Top":
                                layer.Height = Math.Max(10, startH - deltaY);
                                layer.Y = wmStartPos.Y + (startH - layer.Height) / 2.0;
                                break;
                            case "Bottom":
                                layer.Height = Math.Max(10, startH + deltaY);
                                layer.Y = wmStartPos.Y + (layer.Height - startH) / 2.0;
                                break;
                            case "TopLeft":
                                layer.Width = Math.Max(10, startW - deltaX);
                                layer.X = wmStartPos.X + (startW - layer.Width) / 2.0;
                                layer.Height = Math.Max(10, startH - deltaY);
                                layer.Y = wmStartPos.Y + (startH - layer.Height) / 2.0;
                                break;
                            case "BottomRight":
                                layer.Width = Math.Max(10, startW + deltaX);
                                layer.X = wmStartPos.X + (layer.Width - startW) / 2.0;
                                layer.Height = Math.Max(10, startH + deltaY);
                                layer.Y = wmStartPos.Y + (layer.Height - startH) / 2.0;
                                break;
                            case "TopRight":
                                layer.Width = Math.Max(10, startW + deltaX);
                                layer.X = wmStartPos.X + (layer.Width - startW) / 2.0;
                                layer.Height = Math.Max(10, startH - deltaY);
                                layer.Y = wmStartPos.Y + (startH - layer.Height) / 2.0;
                                break;
                            case "BottomLeft":
                                layer.Width = Math.Max(10, startW - deltaX);
                                layer.X = wmStartPos.X + (startW - layer.Width) / 2.0;
                                layer.Height = Math.Max(10, startH + deltaY);
                                break;
                        }

                        SyncWatermarkSizes(layer, IsOneSizeOn, IsAllWatermarksMode);

                        ApplyWatermarkAlignment(layer, isDragging: false);

                        if (IsSameIdAlignmentOn)
                        {
                            SyncWatermarkPositions(layer);
                        }

                        foreach (UIElement child in WatermarksCanvas.Children)
                        {
                            if (child is Border b && b.Tag is WatermarkLayer l)
                            {
                                Rect rect = GetScreenRect(l.X, l.Y, l.Width, l.Height);
                                b.Width = rect.Width;
                                b.Height = rect.Height;
                                Canvas.SetLeft(b, rect.X);
                                Canvas.SetTop(b, rect.Y);
                            }
                        }
                    }
                };

                wmBorder.MouseLeftButtonUp += (s, e) =>
                {
                    isDragging = isResizing = false;
                    wmBorder.ReleaseMouseCapture();
                            UpdateAndTriggerAlignment();
                    e.Handled = true;
                };

                WatermarksCanvas.Children.Add(wmBorder);
            }
        }

        private void WatermarksCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == WatermarksCanvas)
            {
                if (_selectedWatermarkBorder != null)
                {
                    _selectedWatermarkBorder.BorderBrush = Brushes.Transparent;
                    _selectedWatermarkBorder = null;
                }
            }
        }
        private Rect GetScreenRect(double x, double y, double width, double height)
        {
            if (CurrentBitmap == null)
                return Rect.Empty;

            double scaleX = DisplayImage.ActualWidth / CurrentBitmap.PixelWidth;

            double scaleY = DisplayImage.ActualHeight / CurrentBitmap.PixelHeight;

            double uniformScale = Math.Min(scaleX, scaleY);

            double screenWidth = width * uniformScale;
            double screenHeight = height * uniformScale;

            double centerScreenX = DisplayImage.ActualWidth / 2.0;
            double centerScreenY = DisplayImage.ActualHeight / 2.0;

            double screenX = centerScreenX + (x * uniformScale) - (screenWidth / 2.0);

            double screenY = centerScreenY + (y * uniformScale) - (screenHeight / 2.0);

            return new Rect(
                screenX,
                screenY,
                screenWidth,
                screenHeight
            );
        }

    }
}
