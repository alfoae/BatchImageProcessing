using ImageProcessing.Properties;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

public static class ImageService
{
    private static Image? _targetImage;
    public static event Action<BitmapImage>? CurrentImageChanged;
    public static event Action<Int32Rect>? CurrentCropChanged;
    public static BitmapImage? CurrentBitmap { get; private set; }
    public static ListBox? _thumbnailList;
    public static List<string> _imageFiles = new List<string>();
    private static int _currentIndex = -1;
    public static int CurrentIndex;
    private static readonly string[] _extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

    public static Dictionary<string, Int32Rect> _imageCrops = new Dictionary<string, Int32Rect>();

    public static string SelectedQuality { get; set; } = "1080p";
    public static string SelectedFormat { get; set; } = "PNG";

    public static bool IsGlobalCropSize { get; set; } = false;

    public static int GlobalWidth { get; set; } = 100;
    public static int GlobalHeight { get; set; } = 100;

    public static bool IsGlobalCropPosition { get; set; } = false;
    public static int GlobalX { get; set; } = 0;
    public static int GlobalY { get; set; } = 0;

    public static bool IsCropAlignmentOn { get; set; } = false;

    public static int CropAlignmentIndex { get; set; } = 0;

    public static double PreviewScale { get; set; }

    public static double PreviewCenterX { get; set; }

    public static double PreviewCenterY { get; set; }

    private static readonly Dictionary<string, bool> _imageAlignmentEnabled = new Dictionary<string, bool>();
    private static readonly Dictionary<string, string> _imageAlignmentType = new Dictionary<string, string>();

    public static event Action? CurrentAlignmentChanged;

    private static Int32Rect CenteredToSystem(Int32Rect centered, int imgW, int imgH)
    {
        int w = centered.Width;
        int h = centered.Height;
        double halfImgW = imgW / 2.0;
        double halfImgH = imgH / 2.0;

        int sysX = (int)Math.Round((centered.X + halfImgW) - w / 2.0);
        int sysY = (int)Math.Round((centered.Y + halfImgH) - h / 2.0);

        if (sysX < 0) sysX = 0;
        if (sysY < 0) sysY = 0;
        if (sysX + w > imgW) sysX = Math.Max(0, imgW - w);
        if (sysY + h > imgH) sysY = Math.Max(0, imgH - h);

        return new Int32Rect(sysX, sysY, w, h);
    }

    private static Int32Rect SystemToCentered(Int32Rect system, int imgW, int imgH)
    {
        int w = system.Width;
        int h = system.Height;
        double halfImgW = imgW / 2.0;
        double halfImgH = imgH / 2.0;

        int userX = (int)Math.Round((system.X + w / 2.0) - halfImgW);
        int userY = (int)Math.Round((system.Y + h / 2.0) - halfImgH);

        return new Int32Rect(userX, userY, w, h);
    }

    public static void RecalculateCropAlignment()
    {
        if (CurrentBitmap == null || !IsCropAlignmentOn) return;

        int imgW = CurrentBitmap.PixelWidth;
        int imgH = CurrentBitmap.PixelHeight;

        int w = CurrentCrop.Width;
        int h = CurrentCrop.Height;

        if (w <= 0) w = 100;
        if (h <= 0) h = 100;

        int x = 0;
        int y = 0;

        switch (CropAlignmentIndex)
        {
            case 0:
                x = 0;
                y = 0;
                break;
            case 1:
                x = 0;
                y = (h - imgH) / 2;
                break;
            case 2:
                x = 0;
                y = (imgH - h) / 2;
                break;
            case 3:
                x = (w - imgW) / 2;
                y = 0;
                break;
            case 4:
                x = (imgW - w) / 2;
                y = 0;
                break;
            case 5:
                x = (w - imgW) / 2;
                y = (h - imgH) / 2;
                break;
            case 6:
                x = (imgW - w) / 2;
                y = (h - imgH) / 2;
                break;
            case 7:
                x = (w - imgW) / 2;
                y = (imgH - h) / 2;
                break;
            case 8:
                x = (imgW - w) / 2;
                y = (imgH - h) / 2;
                break;
            default:
                x = 0;
                y = 0;
                break;
        }

        int maxXOffset = Math.Abs((imgW - w) / 2);
        int maxYOffset = Math.Abs((imgH - h) / 2);

        if (x > maxXOffset) x = maxXOffset;
        if (x < -maxXOffset) x = -maxXOffset;

        if (y > maxYOffset) y = maxYOffset;
        if (y < -maxYOffset) y = -maxYOffset;

        CurrentCrop = new Int32Rect(x, y, w, h);
    }

    public static void Initialize(Image target, ListBox thumbnails)
    {
        _targetImage = target;
        _thumbnailList = thumbnails;

        _thumbnailList.SelectionChanged += (s, e) =>
        {
            if (_thumbnailList.SelectedIndex >= 0)
            {
                _currentIndex = _thumbnailList.SelectedIndex;
                string SetPath = _imageFiles[_currentIndex];
                BitmapImage bitmap = new BitmapImage(new Uri(SetPath));
                CurrentBitmap = bitmap;
                _targetImage.Source = bitmap;

                CurrentImageChanged?.Invoke(bitmap);
            }
        };

        Settings.Default.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Settings.Default.LastSelectedPathEnter))
            {
                RefreshFileList();
            }
        };
    }

    public static void LoadImages(Image displayImage)
    {
        _imageCrops.Clear();
        try
        {
            string folderPath = Settings.Default.LastSelectedPathEnter;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
            var firstFile = Directory.EnumerateFiles(folderPath)
                .FirstOrDefault(f => extensions.Contains(Path.GetExtension(f).ToLower()));

            if (firstFile != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(firstFile);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.EndInit();
                bitmap.Freeze();
                displayImage.Source = bitmap;
                ApplyZoom(displayImage, 100.0);
            }
        }
        catch
        {

        }
    }

    public static void ApplyZoom(FrameworkElement targetElement, double zoomPercent)
    {
        if (targetElement == null) return;

        double clampedValue = Math.Clamp(zoomPercent, 12.5, 800.0);
        double scale = clampedValue / 100.0;

        targetElement.HorizontalAlignment = HorizontalAlignment.Center;
        targetElement.VerticalAlignment = VerticalAlignment.Center;

        targetElement.RenderTransformOrigin = new Point(0.5, 0.5);

        ScaleTransform st = new ScaleTransform(scale, scale);

        targetElement.LayoutTransform = st;
    }

    public static void CenterImage(Image displayImage)
    {
        if (displayImage == null) return;

        displayImage.HorizontalAlignment = HorizontalAlignment.Center;
        displayImage.VerticalAlignment = VerticalAlignment.Center;

        displayImage.RenderTransformOrigin = new Point(0.5, 0.5);

        if (displayImage.RenderTransform is TransformGroup group)
        {
            var translate = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (translate != null)
            {
                translate.X = 0;
                translate.Y = 0;
            }
        }
    }

    public static void RefreshFileList(ListBox? thumbnailList = null)
    {
        if (thumbnailList != null) _thumbnailList = thumbnailList;

        string folderPath = Settings.Default.LastSelectedPathEnter;
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            _imageFiles.Clear();
            _currentIndex = -1;
            if (_thumbnailList != null) _thumbnailList.ItemsSource = null;
            return;
        }

        _imageFiles = Directory.EnumerateFiles(folderPath).Where(f => _extensions.Contains(Path.GetExtension(f).ToLower())).OrderBy(f => f).ToList();

        if (_thumbnailList != null)
        {
            _thumbnailList.ItemsSource = null;
            _thumbnailList.ItemsSource = _imageFiles;
            _thumbnailList.UpdateLayout();

            if (_imageFiles.Count > 0)
            {
                _currentIndex = CurrentIndex;
                _thumbnailList.SelectedIndex = CurrentIndex;
            }
        }

        PreloadAllCrops();
    }

    public static void LoadImageByIndex(int index)
    {
        CurrentIndex = index;

        if (_targetImage == null || _imageFiles.Count == 0) return;

        if (index < 0) index = _imageFiles.Count - 1;
        if (index >= _imageFiles.Count) index = 0;

        _currentIndex = index;

        try
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(_imageFiles[_currentIndex]);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.EndInit();
            bitmap.Freeze();

            _targetImage.Source = bitmap;
            CurrentBitmap = bitmap;
            CurrentImageChanged?.Invoke(bitmap);
            ApplyZoom(_targetImage, 100.0);
        }
        catch
        {

        }
    }
    public static void Navigate(bool next)
    {
        if (_imageFiles.Count == 0 || _thumbnailList == null) return;

        int newIndex = next ? _currentIndex + 1 : _currentIndex - 1;

        if (newIndex < 0) newIndex = _imageFiles.Count - 1;
        if (newIndex >= _imageFiles.Count) newIndex = 0;

        _thumbnailList.SelectedIndex = newIndex;
        _thumbnailList.ScrollIntoView(_thumbnailList.SelectedItem);
    }

    public static Int32Rect CurrentCrop
    {
        get
        {
            if (_currentIndex >= 0 && _currentIndex < _imageFiles.Count && CurrentBitmap != null)
            {
                string currentFile = _imageFiles[_currentIndex];

                if (!_imageCrops.TryGetValue(currentFile, out var crop))
                {
                    crop = new Int32Rect(0, 0, GlobalWidth, GlobalHeight);
                    _imageCrops[currentFile] = crop;
                }

                int w = IsGlobalCropSize ? GlobalWidth : crop.Width;
                int h = IsGlobalCropSize ? GlobalHeight : crop.Height;

                if (IsGlobalCropPosition)
                {
                    int gx = GlobalX;
                    int gy = GlobalY;
                    return new Int32Rect(gx, gy, w, h);
                }

                int userX = crop.X;
                int userY = crop.Y;

                return new Int32Rect(userX, userY, w, h);
            }

            return Int32Rect.Empty;
        }
        set
        {
            if (_currentIndex >= 0 && _currentIndex < _imageFiles.Count && CurrentBitmap != null)
            {
                string currentFile = _imageFiles[_currentIndex];

                int userX = value.X;
                int userY = value.Y;

                var finalCrop = new Int32Rect(userX, userY, value.Width, value.Height);
                _imageCrops[currentFile] = finalCrop;

                if (IsGlobalCropSize)
                {
                    GlobalWidth = value.Width;
                    GlobalHeight = value.Height;
                }

                if (IsGlobalCropPosition)
                {
                    GlobalX = userX;
                    GlobalY = userY;
                }

                CurrentCropChanged?.Invoke(new Int32Rect(userX, userY, value.Width, value.Height));
            }
        }
    }

    public static void CopyGlobalSizeToAllImages()
    {
        var keys = _imageCrops.Keys.ToList();
        foreach (var file in keys)
        {
            try
            {
                int imgW, imgH;
                using (var bmp = new System.Drawing.Bitmap(file))
                {
                    imgW = bmp.Width;
                    imgH = bmp.Height;
                }

                var crop = _imageCrops[file];
                crop.Width = GlobalWidth;
                crop.Height = GlobalHeight;

                crop.X = GlobalX;
                crop.Y = GlobalY;

                _imageCrops[file] = crop;
            }
            catch { }
        }
    }

    public static void CopyGlobalPositionToAllImages()
    {
        var keys = _imageCrops.Keys.ToList();
        foreach (var file in keys)
        {
            try
            {
                var crop = _imageCrops[file];
                crop.X = GlobalX;
                crop.Y = GlobalY;
                _imageCrops[file] = crop;
            }
            catch { }
        }
    }

    public static Int32Rect GetCropForFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return Int32Rect.Empty;

        bool hasSaved = _imageCrops.TryGetValue(filePath, out Int32Rect centeredCrop);

        if (!hasSaved && !IsGlobalCropSize && !IsGlobalCropPosition)
        {
            return Int32Rect.Empty;
        }

        int imgW = 0, imgH = 0;
        try
        {
            using var info = new System.Drawing.Bitmap(filePath);
            imgW = info.Width;
            imgH = info.Height;
        }
        catch { return Int32Rect.Empty; }

        int w = IsGlobalCropSize ? GlobalWidth : (hasSaved ? centeredCrop.Width : GlobalWidth);
        int h = IsGlobalCropSize ? GlobalHeight : (hasSaved ? centeredCrop.Height : GlobalHeight);

        if (IsGlobalCropPosition)
        {
            int sysX = (int)Math.Round(imgW / 2.0 + GlobalX - w / 2.0);
            int sysY = (int)Math.Round(imgH / 2.0 + GlobalY - h / 2.0);
            if (sysX < 0) sysX = 0;
            if (sysY < 0) sysY = 0;
            if (sysX + w > imgW) sysX = Math.Max(0, imgW - w);
            if (sysY + h > imgH) sysY = Math.Max(0, imgH - h);
            return new Int32Rect(sysX, sysY, w, h);
        }
        else if (hasSaved)
        {
            var centered = new Int32Rect(centeredCrop.X, centeredCrop.Y, w, h);
            return CenteredToSystem(centered, imgW, imgH);
        }
        else
        {
            int sysX = (int)Math.Round((imgW - w) / 2.0);
            int sysY = (int)Math.Round((imgH - h) / 2.0);
            return new Int32Rect(sysX, sysY, w, h);
        }
    }

    public static bool IsCurrentAlignmentEnabled
    {
        get
        {
            if (_currentIndex >= 0 && _currentIndex < _imageFiles.Count)
            {
                string file = _imageFiles[_currentIndex];
                return _imageAlignmentEnabled.TryGetValue(file, out bool enabled) && enabled;
            }
            return false;
        }
        set
        {
            if (_currentIndex >= 0 && _currentIndex < _imageFiles.Count)
            {
                string file = _imageFiles[_currentIndex];
                _imageAlignmentEnabled[file] = value;

                if (value)
                {
                    CurrentCrop = CurrentCrop;
                }
                CurrentAlignmentChanged?.Invoke();
            }
        }
    }

    public static string CurrentAlignmentType
    {
        get
        {
            if (_currentIndex >= 0 && _currentIndex < _imageFiles.Count)
            {
                string file = _imageFiles[_currentIndex];
                return _imageAlignmentType.TryGetValue(file, out string type) ? type : "Центру";
            }
            return "Центру";
        }
        set
        {
            if (_currentIndex >= 0 && _currentIndex < _imageFiles.Count)
            {
                string file = _imageFiles[_currentIndex];
                _imageAlignmentType[file] = value;

                if (IsCurrentAlignmentEnabled)
                {
                    CurrentCrop = CurrentCrop;
                }
                CurrentAlignmentChanged?.Invoke();
            }
        }
    }

    public static void SelectNextImage()
    {
        if (_imageFiles.Count == 0) return;

        int nextIndex = CurrentIndex + 1;
        if (nextIndex >= _imageFiles.Count) nextIndex = 0;

        CurrentIndex = nextIndex;
    }

    public static void SelectPreviousImage()
    {
        if (_imageFiles.Count == 0) return;

        int prevIndex = CurrentIndex - 1;
        if (prevIndex < 0) prevIndex = _imageFiles.Count - 1;

        CurrentIndex = prevIndex;
    }

    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK
    //WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK ----- WATER MARK

    public static event Action? WatermarksChanged;
    public static event Action? WatermarkAlignmentChanged;
    public static bool IsOneSizeOn { get; set; } = false;
    public static bool IsAllWatermarksMode { get; set; } = false;
    public static bool IsSameIdAlignmentOn { get; set; } = false;
    public static WatermarkLayer? SelectedWatermark { get; set; }
    public static bool IsWatermarkAlignmentEnabled { get; set; } = false;
    public static bool IsAllWatermarksOpacityMode { get; set; } = false;
    public static int GlobalOrderCounter { get; set; } = 0;

    public static BitmapImage? GetWatermarkBitmap()
    {
        try
        {
            string path = ImageProcessing.Properties.Settings.Default.WatermarkFilePath;
            if (string.IsNullOrEmpty(path)) return null;

            string? fileToLoad = null;

            if (File.Exists(path))
            {
                fileToLoad = path;
            }
            else if (Directory.Exists(path))
            {
                fileToLoad = Directory.EnumerateFiles(path).FirstOrDefault(f => _extensions.Contains(Path.GetExtension(f).ToLower()));
            }

            if (fileToLoad != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(fileToLoad, UriKind.Absolute);
                bitmap.EndInit();
                return bitmap;
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    public static BitmapImage? GetNextWatermarkBitmap(int direction)
    {
        try
        {
            string currentPath = Settings.Default.WatermarkFilePath;
            if (string.IsNullOrEmpty(currentPath)) return null;

            string directory = File.Exists(currentPath) ? Path.GetDirectoryName(currentPath) : currentPath;
            if (!Directory.Exists(directory)) return null;

            var files = Directory.GetFiles(directory).Where(f => _extensions.Contains(Path.GetExtension(f).ToLower())).OrderBy(f => f).ToList();

            if (files.Count == 0) return null;

            int currentIndex = files.IndexOf(currentPath);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = currentIndex + direction;
            if (newIndex >= files.Count) newIndex = 0;
            if (newIndex < 0) newIndex = files.Count - 1;

            Settings.Default.WatermarkFilePath = files[newIndex];
            Settings.Default.Save();

            return GetWatermarkBitmap();
        }
        catch
        {
            return null;
        }
    }

    public class WatermarkLayer
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public double X { get; set; } = 10;
        public double Y { get; set; } = 10;
        public double Width { get; set; } = 150;
        public double Height { get; set; } = 150;
        public double Opacity { get; set; } = 1.0;
        public bool IsAlignmentEnabled { get; set; } = false;
        public string SelectedAlignment { get; set; } = "Центру";
        public bool IsGlobal { get; set; } = false;
        public int GlobalOrder { get; set; } = 0;
        public string OriginalPhotoPath { get; set; } = string.Empty;
        public bool UseAlignment { get; set; }
        public int AlignmentIndex { get; set; }
    }

    public static string GetCurrentPhotoPath()
    {
        if (CurrentIndex >= 0 && CurrentIndex < _imageFiles.Count)
        {
            return _imageFiles[CurrentIndex];
        }
        return string.Empty;
    }

    public static Dictionary<string, List<WatermarkLayer>> ImageWatermarks { get; set; } = new Dictionary<string, List<WatermarkLayer>>();

    public static List<WatermarkLayer> GetCurrentPhotoLayers()
    {
        string currentFile = _image_files_safe();
        if (string.IsNullOrEmpty(currentFile)) return new List<WatermarkLayer>();

        if (!ImageWatermarks.ContainsKey(currentFile))
            ImageWatermarks[currentFile] = new List<WatermarkLayer>();

        return ImageWatermarks[currentFile];
    }

    private static string _image_files_safe()
    {
        return _imageFiles.ElementAtOrDefault(CurrentIndex) ?? string.Empty;
    }

    public static WatermarkLayer AddNewWatermark(string currentWatermarkPath)
    {
        var layers = GetCurrentPhotoLayers();
        int newId = GenerateNextLocalId(layers);

        string currentFile = _image_files_safe();

        var newLayer = new WatermarkLayer
        {
            Id = newId,
            ImagePath = currentWatermarkPath,
            X = 0,
            Y = 0,
            Width = 150,
            Height = 150,
            OriginalPhotoPath = currentFile,
            IsAlignmentEnabled = false,
            SelectedAlignment = "Центру",
            IsGlobal = false,
            GlobalOrder = 0
        };
        layers.Add(newLayer);
        WatermarksChanged?.Invoke();
        return newLayer;
    }

    public static List<string> GetAllImageFiles()
    {
        return _imageFiles;
    }

    public static void PreloadAllCrops()
    {
        foreach (var filePath in _imageFiles)
        {
            if (_imageCrops.ContainsKey(filePath)) continue;
            try
            {
                int imgW, imgH;
                using (var bmp = new System.Drawing.Bitmap(filePath))
                {
                    imgW = bmp.Width;
                    imgH = bmp.Height;
                }
                int w = IsGlobalCropSize ? GlobalWidth : Math.Min(GlobalWidth, imgW);
                int h = IsGlobalCropSize ? GlobalHeight : Math.Min(GlobalHeight, imgH);
                int sysX, sysY;
                if (IsGlobalCropPosition)
                {
                    sysX = (int)Math.Round(imgW / 2.0 + GlobalX - w / 2.0);
                    sysY = (int)Math.Round(imgH / 2.0 + GlobalY - h / 2.0);
                }
                else
                {
                    sysX = (int)Math.Round((imgW - w) / 2.0);
                    sysY = (int)Math.Round((imgH - h) / 2.0);
                }
                var centered = SystemToCentered(new Int32Rect(sysX, sysY, w, h), imgW, imgH);
                _imageCrops[filePath] = centered;
            }
            catch { }
        }
    }

    public static void SyncWatermarkSizes(WatermarkLayer baseLayer, bool isOneSizeOn, bool isAllWatermarksMode)
    {
        if (!isOneSizeOn || baseLayer == null) return;

        double targetWidth = baseLayer.Width;
        double targetHeight = baseLayer.Height;

        foreach (var photoPath in ImageWatermarks.Keys)
        {
            var targetLayers = ImageWatermarks[photoPath];
            if (targetLayers == null) continue;

            foreach (var layer in targetLayers)
            {
                if (layer == baseLayer) continue;

                if (isAllWatermarksMode || layer.Id == baseLayer.Id)
                {
                    layer.Width = targetWidth;
                    layer.Height = targetHeight;
                }
            }
        }
    }
    public static void SyncWatermarkPositions(WatermarkLayer baseLayer)
    {
        if (baseLayer == null || !IsSameIdAlignmentOn) return;

        foreach (var photoPath in ImageWatermarks.Keys)
        {
            var targetLayers = ImageWatermarks[photoPath];
            if (targetLayers == null) continue;

            foreach (var layer in targetLayers)
            {
                if (layer == baseLayer) continue;

                if (layer.Id == baseLayer.Id)
                {
                    layer.UseAlignment = baseLayer.UseAlignment;
                    layer.AlignmentIndex = baseLayer.AlignmentIndex;
                    layer.SelectedAlignment = baseLayer.SelectedAlignment;

                    if (baseLayer.UseAlignment)
                    {
                        ApplyWatermarkAlignment(layer, isDragging: false, photoPath: photoPath);
                    }
                    else
                    {
                        layer.X = baseLayer.X;
                        layer.Y = baseLayer.Y;
                        ApplyWatermarkAlignment(layer, isDragging: false, photoPath: photoPath);
                    }
                }
            }
        }
    }

    public static void ApplyWatermarkAlignment(WatermarkLayer layer, bool isDragging = false, double startX = 0, double startY = 0, string? photoPath = null)
    {
        if (layer == null) return;

        double imgW = 0;
        double imgH = 0;

        try
        {
            if (!string.IsNullOrEmpty(photoPath) && System.IO.File.Exists(photoPath))
            {
                var bitmapDecoder = BitmapDecoder.Create(new Uri(photoPath), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                var frame = bitmapDecoder.Frames[0];
                imgW = frame.PixelWidth;
                imgH = frame.PixelHeight;
            }
            else
            {
                if (CurrentBitmap == null) return;
                imgW = CurrentBitmap.PixelWidth;
                imgH = CurrentBitmap.PixelHeight;
            }
        }
        catch
        {
            if (CurrentBitmap == null) return;
            imgW = CurrentBitmap.PixelWidth;
            imgH = CurrentBitmap.PixelHeight;
        }

        double minX = -(imgW / 2.0) + (layer.Width / 2.0);
        double maxX = (imgW / 2.0) - (layer.Width / 2.0);
        double minY = -(imgH / 2.0) + (layer.Height / 2.0);
        double maxY = (imgH / 2.0) - (layer.Height / 2.0);

        if (minX > maxX) minX = maxX = 0;
        if (minY > maxY) minY = maxY = 0;

        if (layer.UseAlignment)
        {
            switch (layer.AlignmentIndex)
            {
                case 0:
                    layer.X = 0; layer.Y = 0;
                    break;
                case 1:
                    layer.X = 0; layer.Y = minY;
                    break;
                case 2:
                    layer.X = 0; layer.Y = maxY;
                    break;
                case 3:
                    layer.X = minX; layer.Y = 0;
                    break;
                case 4:
                    layer.X = maxX; layer.Y = 0;
                    break;
                case 5:
                    layer.X = minX; layer.Y = minY;
                    break;
                case 6:
                    layer.X = maxX; layer.Y = minY;
                    break;
                case 7:
                    layer.X = minX; layer.Y = maxY;
                    break;
                case 8:
                    layer.X = maxX; layer.Y = maxY;
                    break;

                case 9:
                    if (isDragging) layer.X = startX;
                    break;

                case 10:
                    if (isDragging) layer.Y = startY;
                    break;
            }
        }

        bool applyBoundsRestriction = layer.UseAlignment && layer.AlignmentIndex != 9 && layer.AlignmentIndex != 10;

        if (applyBoundsRestriction)
        {
            layer.X = Math.Clamp(layer.X, minX, maxX);
            layer.Y = Math.Clamp(layer.Y, minY, maxY);
        }
        else
        {
            layer.X = Math.Clamp(layer.X, -imgW, imgW);
            layer.Y = Math.Clamp(layer.Y, -imgH, imgH);
        }
    }

    public static void UpdateAllWatermarksAlignment()
    {
        var layers = GetCurrentPhotoLayers();
        foreach (var layer in layers)
        {
            ApplyWatermarkAlignment(layer, isDragging: false);
        }
        WatermarkAlignmentChanged?.Invoke();
    }

    public static void UpdateSelectedWatermarkAlignment()
    {
        if (SelectedWatermark == null) return;

        ApplyWatermarkAlignment(SelectedWatermark, isDragging: false);

        if (IsSameIdAlignmentOn)
        {
            SyncWatermarkPositions(SelectedWatermark);
        }

        WatermarkAlignmentChanged?.Invoke();
    }
    public static void UpdateAndTriggerAlignment()
    {
        var layers = GetCurrentPhotoLayers();
        if (layers != null)
        {
            foreach (var layer in layers)
            {
                ApplyWatermarkAlignment(layer, isDragging: false);
                if (IsSameIdAlignmentOn)
                {
                    SyncWatermarkPositions(layer);
                }
            }
            WatermarkAlignmentChanged?.Invoke();
        }
    }

    public static void SyncWatermarkOpacityById(WatermarkLayer baseLayer)
    {
        if (baseLayer == null) return;

        foreach (var photoPath in ImageWatermarks.Keys)
        {
            var targetLayers = ImageWatermarks[photoPath];
            if (targetLayers == null) continue;

            foreach (var layer in targetLayers)
            {
                if (layer == baseLayer) continue;

                if (layer.Id == baseLayer.Id)
                {
                    layer.Opacity = baseLayer.Opacity;
                }
            }
        }
    }

    public static void SyncAllWatermarksOpacity(double opacity)
    {
        foreach (var photoPath in ImageWatermarks.Keys)
        {
            var targetLayers = ImageWatermarks[photoPath];
            if (targetLayers == null) continue;

            foreach (var layer in targetLayers)
            {
                layer.Opacity = opacity;
            }
        }
    }
    private static int GenerateNextGlobalId()
    {
        int minId = 0;
        foreach (var list in ImageWatermarks.Values)
        {
            foreach (var layer in list)
            {
                if (layer.IsGlobal && layer.Id <= minId)
                {
                    minId = layer.Id - 1;
                }
            }
        }
        return minId;
    }

    private static int GenerateNextLocalId(List<WatermarkLayer> layers)
    {
        int id = 1;
        while (layers.Any(l => l.Id == id))
        {
            id++;
        }
        return id;
    }

    public static void ToggleGlobalWatermark(WatermarkLayer layer)
    {
        if (layer == null) return;

        string currentFile = GetCurrentPhotoPath();
        if (string.IsNullOrEmpty(currentFile)) return;

        if (!layer.IsGlobal)
        {
            layer.IsGlobal = true;
            GlobalOrderCounter++;
            layer.GlobalOrder = GlobalOrderCounter;
            layer.Id = GenerateNextGlobalId();

            var allFiles = GetAllImageFiles();
            foreach (var file in allFiles)
            {
                if (file == currentFile) continue;

                if (!ImageWatermarks.ContainsKey(file))
                    ImageWatermarks[file] = new List<WatermarkLayer>();

                if (!ImageWatermarks[file].Any(l => l.IsGlobal && l.GlobalOrder == layer.GlobalOrder))
                {
                    var clone = new WatermarkLayer
                    {
                        Id = layer.Id,
                        ImagePath = layer.ImagePath,
                        X = layer.X,
                        Y = layer.Y,
                        Width = layer.Width,
                        Height = layer.Height,
                        Opacity = layer.Opacity,
                        IsAlignmentEnabled = layer.IsAlignmentEnabled,
                        SelectedAlignment = layer.SelectedAlignment,
                        IsGlobal = true,
                        GlobalOrder = layer.GlobalOrder,
                        OriginalPhotoPath = layer.OriginalPhotoPath
                    };
                    ImageWatermarks[file].Add(clone);
                    ApplyWatermarkAlignment(clone, isDragging: false, photoPath: file);
                }
            }
        }
        else
        {
            int previousGlobalOrder = layer.GlobalOrder;

            layer.IsGlobal = false;
            layer.GlobalOrder = 0;

            var currentLayers = GetCurrentPhotoLayers();
            layer.Id = GenerateNextLocalId(currentLayers);

            foreach (var photoPath in ImageWatermarks.Keys)
            {
                if (photoPath == currentFile) continue;

                ImageWatermarks[photoPath].RemoveAll(l => l.IsGlobal && l.GlobalOrder == previousGlobalOrder);
            }
        }

        WatermarksChanged?.Invoke();
    }
    public static List<WatermarkLayer> GetSortedLayersForUI()
    {
        var layers = GetCurrentPhotoLayers();
        return layers.OrderByDescending(l => l.IsGlobal).ThenBy(l => l.IsGlobal ? l.GlobalOrder : 0).ThenBy(l => l.Id).ToList();
    }

    public static void NotifyWatermarksChanged()
    {
        WatermarksChanged?.Invoke();
    }

}