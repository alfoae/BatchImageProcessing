using ImageProcessing.Models;
using ImageProcessing.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ImageService;

namespace ImageProcessing
{
    public partial class Settings : Page
    {
        private bool _isDragging = false;
        private Point _clickPosition;
        private bool _isUpdatingUI = false;

        private ListBox _thumbnails;
        private bool _isInitializing = false;

        private readonly Dictionary<string, string> _wmPhotoProfiles = new Dictionary<string, string>();


        public Settings(ListBox thumbnails)
        {
            _isInitializing = true;

            InitializeComponent();

            LoadProfiles();
            LoadWatermarkProfiles();

            for (int i = 1; i <= 4; i++)
            {
                streams.Items.Add(i);
            }
            

            streams.SelectedItem = Properties.Settings.Default.ThreadCount;

            streams.SelectedIndex = 0;

            SaveCurrentSettings();

            OneSize.IsChecked = IsOneSizeOn;
            OneSize.Content = IsOneSizeOn ? "ON" : "OFF";
            ComboWatermarkScope.SelectedIndex = IsAllWatermarksMode ? 1 : 0;

            WatermarkPreviewImage.Source = GetWatermarkBitmap();
            _thumbnails = thumbnails;
            CurrentImageChanged += UpdatePreviewImage;
            CurrentImageChanged += (bitmap) => 
            { 
                RefreshWatermarkComboBox();
                RenderPreviewWatermarks();
            };
            M1FolderEnter.Text = Properties.Settings.Default.LastSelectedPathEnter;
            M1FolderOut.Text = Properties.Settings.Default.LastSelectedPathOut;
            M2WatermarkPath.Text = Properties.Settings.Default.WatermarkFilePath;

            Loaded += (s, e) =>
            {
                LoadCurrentWatermarkToUI();
                RenderPreviewWatermarks();
            };

            CurrentImageChanged += (bitmap) =>
            {
                LoadCurrentCropToUI();
            };

            CurrentImageChanged += (bitmap) =>
            {
                string photoPath = GetCurrentPhotoPath();

                if (ApplyToAllWatermark.IsChecked == true)
                {
                    string? anyProfile = _wmPhotoProfiles.Values.FirstOrDefault();
                    if (anyProfile != null)
                    {
                        _wmPhotoProfiles[photoPath] = anyProfile;
                        ApplyWatermarkProfileByName(anyProfile);
                    }
                }

                _isUpdatingUI = true;
                if (!string.IsNullOrEmpty(photoPath) && _wmPhotoProfiles.TryGetValue(photoPath, out string? savedName))
                {
                    MyComboBoxN3Watermark.SelectedItem = savedName;
                    if (MyComboBoxN3Watermark.SelectedIndex < 0)
                        MyComboBoxN3Watermark.Text = savedName;
                }
                else
                {
                    MyComboBoxN3Watermark.SelectedIndex = -1;
                    MyComboBoxN3Watermark.Text = string.Empty;
                }
                _isUpdatingUI = false;
            };

            WatermarkAlignmentChanged += () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RenderPreviewWatermarks();
                    UpdateCoordinateTextBoxes();
                }), System.Windows.Threading.DispatcherPriority.Render);
            };

            CurrentCropChanged += (crop) =>
            {
                if (_isUpdatingUI) return;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadCurrentCropToUI();
                }), System.Windows.Threading.DispatcherPriority.Render);
            };

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    LoadCurrentCropToUI();
                }
            };

            if (CurrentBitmap != null)
            {
                UpdatePreviewImage(CurrentBitmap);
                LoadCurrentCropToUI();
            }

            CurrentAlignmentChanged += () =>
            {
                UpdateAlignmentUIState();
            };

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    LoadCurrentCropToUI();
                    UpdateAlignmentUIState();
                }
            };

            if (CurrentBitmap != null)
            {
                UpdatePreviewImage(CurrentBitmap);
                LoadCurrentCropToUI();
                UpdateAlignmentUIState();
            }



            _isInitializing = false;


        }

        private void SquareBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitializing && CurrentBitmap != null)
            {
                UpdatePreviewVisuals();
                RenderPreviewWatermarks();
            }
        }

        private void LoadCurrentCropToUI()
        {
            if (CurrentCrop != Int32Rect.Empty)
            {
                _isInitializing = true;

                PosXBox.Text = CurrentCrop.X.ToString();
                PosYBox.Text = CurrentCrop.Y.ToString();
                WidthBox.Text = CurrentCrop.Width.ToString();
                HeightBox.Text = CurrentCrop.Height.ToString();

                _isInitializing = false;

                UpdatePreviewVisuals();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                UpdatePreviewVisuals();
            }
        }

        public void UpdatePreviewImage(BitmapImage bitmap)
        {
            if (bitmap == null) return;

            SquareBorder.Background = new ImageBrush(bitmap)
            {
                Stretch = Stretch.Uniform,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton)
            {
                if (sender is not TextBox clickedTextBox)
                {
                    throw new Exception("Щось пішло не так :/");
                }
                else
                {
                    string folder;
                    switch (clickedTextBox.Name)
                    {
                        case "M1FolderEnter":
                            folder = M1FolderEnter.Text;
                            break;
                        case "M1FolderOut":
                            folder = M1FolderOut.Text;
                            break;
                        case "M2WatermarkPath":
                            folder = M2WatermarkPath.Text;
                            break;
                        default: throw new Exception("Щось пішло не так :/");
                    }
                    string resrt = RedactorPageService.SelectFolder(sender, e, folder);
                    if (resrt == "reset")
                    {
                        M1FolderEnter.Text = Properties.Settings.Default.LastSelectedPathEnter;
                        M1FolderOut.Text = Properties.Settings.Default.LastSelectedPathOut;
                        M2WatermarkPath.Text = Properties.Settings.Default.WatermarkFilePath;
                    }

                    WatermarkPreviewImage.Source = GetWatermarkBitmap();
                }
            }
            else
            {
                switch (clickedButton.Name)
                {
                    case "ButtonEnter":
                        M1FolderEnter.Text = RedactorPageService.SelectFolder(sender, e, string.Empty);
                        break;
                    case "ButtonOut":
                        M1FolderOut.Text = RedactorPageService.SelectFolder(sender, e, string.Empty);
                        break;
                    case "ButtonWatermarkBrowse":
                        M2WatermarkPath.Text = RedactorPageService.SelectFolder(sender, e, string.Empty);
                        break;
                    default: throw new Exception("Щось пішло не так :/");
                }
            }
        }

        private void UpdatePreviewVisuals()
        {
            if (CurrentBitmap == null || SquareBorder.ActualWidth == 0 || PreviewRectangle == null) return;

            if (!int.TryParse(PosXBox.Text, out int userX)) userX = 0;
            if (!int.TryParse(PosYBox.Text, out int userY)) userY = 0;
            if (!int.TryParse(WidthBox.Text, out int w)) w = 100;
            if (!int.TryParse(HeightBox.Text, out int h)) h = 100;

            double ratio = Math.Min(SquareBorder.ActualWidth / CurrentBitmap.PixelWidth, SquareBorder.ActualHeight / CurrentBitmap.PixelHeight);

            double renderedWidth = CurrentBitmap.PixelWidth * ratio;
            double renderedHeight = CurrentBitmap.PixelHeight * ratio;

            double offsetX = (SquareBorder.ActualWidth - renderedWidth) / 2;
            double offsetY = (SquareBorder.ActualHeight - renderedHeight) / 2;

            PreviewRectangle.Width = w * ratio;
            PreviewRectangle.Height = h * ratio;

            double finalLeft = (CurrentBitmap.PixelWidth / 2.0 + userX) * ratio + offsetX - (PreviewRectangle.Width / 2.0);
            double finalTop = (CurrentBitmap.PixelHeight / 2.0 + userY) * ratio + offsetY - (PreviewRectangle.Height / 2.0);

            Canvas.SetLeft(PreviewRectangle, finalLeft);
            Canvas.SetTop(PreviewRectangle, finalTop);

            if (!_isInitializing)
            {
                _isUpdatingUI = true;
                CurrentCrop = new Int32Rect(userX, userY, w, h);
                _isUpdatingUI = false;
            }
        }

        private void TextBoxes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string cleanText = textBox.Text;

                if (textBox.Name == "WidthBox" || textBox.Name == "HeightBox")
                {
                    cleanText = System.Text.RegularExpressions.Regex.Replace(textBox.Text, "[^0-9]", "");
                }
                else if (textBox.Name == "PosXBox" || textBox.Name == "PosYBox")
                {
                    cleanText = System.Text.RegularExpressions.Regex.Replace(textBox.Text, "[^0-9-]", "");

                    if (cleanText.Contains("-"))
                    {
                        cleanText = "-" + cleanText.Replace("-", "");
                        if (cleanText == "-") return;
                    }
                }
                if (textBox.Text != cleanText)
                {
                    int caret = textBox.CaretIndex;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = caret > 0 ? caret - 1 : 0;
                    return;
                }
            }
            if (sender is TextBox tb && tb.Text != "-" && tb.Text != "")
            {
                
                UpdatePreviewVisuals();
            }
        }

        private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePreviewVisuals();
        }

        private void AdjustValue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string[] parts = btn.Tag.ToString().Split('_');
                if (parts.Length == 2)
                {
                    string targetBoxName = parts[0];
                    string action = parts[1];

                    if (IsCurrentAlignmentEnabled)
                    {
                        int alignType = int.Parse(CurrentAlignmentType);

                        if (alignType == 0 && (targetBoxName == "PosXBox" || targetBoxName == "PosYBox"))
                            return;

                        if (alignType == 1 && targetBoxName == "PosXBox")
                            return;

                        if (alignType == 2 && targetBoxName == "PosYBox")
                            return;
                    }

                    TextBox targetBox = null;
                    if (targetBoxName == "HeightBox") targetBox = HeightBox;
                    else if (targetBoxName == "WidthBox") targetBox = WidthBox;
                    else if (targetBoxName == "PosXBox") targetBox = PosXBox;
                    else if (targetBoxName == "PosYBox") targetBox = PosYBox;

                    string textValue = (targetBox != null && targetBox.Text == "-") ? "0" : targetBox?.Text;

                    if (targetBox != null && int.TryParse(textValue, out int currentValue))
                    {
                        if (action == "Up")
                        {
                            currentValue++;
                        }
                        else if (action == "Down")
                        {
                            if ((targetBoxName == "WidthBox" || targetBoxName == "HeightBox") && currentValue <= 1)
                                return;

                            currentValue--;
                        }

                        targetBox.Text = currentValue.ToString();
                    }
                }
            }
        }
        private void MyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyComboBox != null && MyComboBox.SelectedItem is ComboBoxItem item)
            {
                SelectedQuality = item.Content?.ToString() ?? "1080p";
            }
        }

        private void MyComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyComboBox1 != null && MyComboBox1.SelectedItem is ComboBoxItem item)
            {
                SelectedFormat = item.Content?.ToString() ?? "PNG";
            }
        }

        private void SaveCurrentSettings()
        {
            SelectedQuality = (MyComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1080p";
            SelectedFormat = (MyComboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PNG";
        }

        private void GlobalSizeSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalSizeSwitch.IsChecked == true)
            {
                if (CurrentCrop != Int32Rect.Empty)
                {
                    GlobalWidth = CurrentCrop.Width;
                    GlobalHeight = CurrentCrop.Height;
                }

                IsGlobalCropSize = true;
                GlobalSizeSwitch.Content = "ON";
            }
            else
            {
                IsGlobalCropSize = false;
                GlobalSizeSwitch.Content = "OFF";

                CopyGlobalSizeToAllImages();
            }

            UpdatePreviewVisuals();
        }

        

        private void AlignmentSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI) return;

            IsCropAlignmentOn = AlignmentSwitch.IsChecked == true;
            AlignmentSwitch.Content = IsCropAlignmentOn ? "ON" : "OFF";
            AlignmentComboBox.IsEnabled = IsCropAlignmentOn;

            if (IsCropAlignmentOn)
            {
                RecalculateCropAlignment();
            }
        }

        private void WMAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;
            if (MyComboBoxN1 == null || WMAlignmentComboBox == null) return;

            var layers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;
            if (layers == null || selectedIndex == -1 || selectedIndex >= layers.Count) return;

            var activeLayer = layers[selectedIndex];

            activeLayer.AlignmentIndex = WMAlignmentComboBox.SelectedIndex;

            if (activeLayer.IsGlobal && ToggleSameIdAlignment.IsChecked == true)
            {
                SyncGlobalAlignmentSettings(activeLayer);
            }
            else
            {
                ApplyWatermarkAlignment(activeLayer, isDragging: false);
            }

            NotifyWatermarksChanged();
            RenderPreviewWatermarks();
        }

        private void UpdateAlignmentUIState()
{
    if (AlignmentSwitch == null || AlignmentComboBox == null) return;

    bool isEnabled = IsCropAlignmentOn;
    int selectedIndex = CropAlignmentIndex;

    bool previouslyUpdating = _isUpdatingUI;
    _isUpdatingUI = true;

    AlignmentSwitch.IsChecked = isEnabled;
    AlignmentSwitch.Content = isEnabled ? "ON" : "OFF";
    AlignmentComboBox.IsEnabled = isEnabled;
    AlignmentComboBox.SelectedIndex = selectedIndex;

    _isUpdatingUI = previouslyUpdating;
}
        


        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;

            SelectPreviousImage();

            if (_thumbnailList != null)
            {
                _thumbnailList.SelectedIndex = CurrentIndex;
            }
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;

            SelectNextImage();

            if (_thumbnailList != null)
            {
                _thumbnailList.SelectedIndex = CurrentIndex;
            }
        }


        private void CropAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI || _isInitializing || AlignmentComboBox == null) return;

            CropAlignmentIndex = AlignmentComboBox.SelectedIndex;

            if (IsCropAlignmentOn)
            {
                RecalculateCropAlignment();
            }
        }

        private void OneAlignmentCheck_Click(object sender, RoutedEventArgs e)
        {
            if (OneAlignmentCheck.IsChecked == true)
            {
                if (CurrentCrop != Int32Rect.Empty)
                {
                    GlobalX = CurrentCrop.X;
                    GlobalY = CurrentCrop.Y;
                }

                IsGlobalCropPosition = true;
                OneAlignmentCheck.Content = "ON";
            }
            else
            {
                IsGlobalCropPosition = false;
                OneAlignmentCheck.Content = "OFF";

                CopyGlobalPositionToAllImages();
            }

            _isInitializing = true;
            _isInitializing = false;

            UpdatePreviewVisuals();
        }

        private void ApplyToAll_Checked(object sender, RoutedEventArgs e)
        {
            ApplyToAll.Content = "Для всіх полів";
        }

        private void ApplyToAll_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyToAll.Content = "Для цього поля";
        }

        private void LoadProfiles()
        {
            MyComboBoxN3.Items.Clear();

            foreach (var profile in ProfileService.Profiles)
            {
                MyComboBoxN3.Items.Add(profile.Name);
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string profileName = MyComboBoxN3.Text;

            if (string.IsNullOrWhiteSpace(profileName))
            {
                MessageBox.Show("Введіть назву профілю");
                return;
            }

            var existing = ProfileService.Profiles
                .FirstOrDefault(x => x.Name == profileName);

            if (existing != null)
            {
                ProfileService.Profiles.Remove(existing);
            }

            Int32Rect crop = CurrentCrop;

            var profile = new ProcessingProfile
            {
                Name = profileName,

                CropX = crop.X,
                CropY = crop.Y,
                CropWidth = crop.Width,
                CropHeight = crop.Height,

                ApplyToAll = ApplyToAll.IsChecked == true
            };

            ProfileService.Profiles.Add(profile);

            ProfileService.Save();

            LoadProfiles();

            MessageBox.Show("Профіль збережено");
        }

        private void MyComboBoxN3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyComboBoxN3.SelectedItem == null)
                return;

            string selected = MyComboBoxN3.SelectedItem.ToString();

            var profile = ProfileService.Profiles
                .FirstOrDefault(x => x.Name == selected);

            if (profile == null)
                return;

            CurrentCrop = new Int32Rect
                (
                profile.CropX,
                profile.CropY,
                profile.CropWidth,
                profile.CropHeight
                );

            ApplyToAll.IsChecked = profile.ApplyToAll;

            MessageBox.Show("Профіль завантажено");
        }

        private void ApplyProfileByName(string profileName)
        {
            var profile = ProfileService.Profiles.FirstOrDefault(x => x.Name == profileName);
            if (profile == null) return;
            ImageService.CurrentCrop = new Int32Rect(
                profile.CropX, profile.CropY, profile.CropWidth, profile.CropHeight);
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (MyComboBoxN3.SelectedItem == null)
                return;

            string selected = MyComboBoxN3.SelectedItem.ToString();

            var profile = ProfileService.Profiles
                .FirstOrDefault(x => x.Name == selected);

            if (profile == null)
                return;

            ProfileService.Profiles.Remove(profile);

            ProfileService.Save();

            LoadProfiles();

            MyComboBoxN3.Text = "";

            MessageBox.Show("Профіль видалено");
        }

        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ
        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ
        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ
        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ
        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ
        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ
        //ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ ДРУГА ПАНЕЛЬКА ДЛЯ ВОДЯНОГО ЗНАКУ

        private void WatermarkBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;
            WatermarkPreviewImage.Source = GetNextWatermarkBitmap(-1);
        }

        private void WatermarkForward_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;
            WatermarkPreviewImage.Source = GetNextWatermarkBitmap(1);
        }

        private void WatermarkToggle_Click(object sender, RoutedEventArgs e)
        {
            if (WatermarkToggle.IsChecked == true)
                WatermarkToggle.Content = "ON";
            else
                WatermarkToggle.Content = "OFF";
        }

        private void ButtonAddWatermark_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            _isUpdatingUI = true;

            try
            {
                string currentPath = Properties.Settings.Default.WatermarkFilePath;
                if (string.IsNullOrEmpty(currentPath) || !System.IO.File.Exists(currentPath))
                {
                    MessageBox.Show("Будь ласка, спочатку оберіть або завантажте файл водяного знаку.");
                    return;
                }

                var newLayer = AddNewWatermark(currentPath);

                if (ToggleGlobalWatermark != null)
                {
                    ToggleGlobalWatermark.IsChecked = false;
                    ToggleGlobalWatermark.Content = "OFF";
                }

                RefreshWatermarkComboBox();

                var layers = GetCurrentPhotoLayers();
                if (layers != null)
                {
                    int targetIndex = layers.FindIndex(l => l.Id == newLayer.Id);
                    if (targetIndex >= 0)
                    {
                        MyComboBoxN1.SelectedIndex = targetIndex;
                    }
                }
            }
            finally
            {
                _isUpdatingUI = false;
            }

            SyncUIWithSelectedWatermark();
            UpdateWatermarkUIFromSelection();
            RenderPreviewWatermarks();
        }

        private void MyComboBoxN1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;

            SyncUIWithSelectedWatermark();
            UpdateWatermarkUIFromSelection();
        }

        private void ButtonRemoveWatermark_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;

            int selectedIndex = MyComboBoxN1.SelectedIndex;
            var layers = GetCurrentPhotoLayers();

            if (selectedIndex == -1 || selectedIndex >= layers.Count) return;

            var activeLayer = layers[selectedIndex];
            int targetId = activeLayer.Id;
            bool isGlobal = activeLayer.IsGlobal;
            string targetOriginalPath = activeLayer.OriginalPhotoPath;
            string currentPhotoPath = GetCurrentPhotoPath();

            layers.RemoveAt(selectedIndex);

            if (isGlobal)
            {
                var allPhotos = GetAllImageFiles();
                if (allPhotos != null)
                {
                    foreach (var photoPath in allPhotos)
                    {
                        if (photoPath == currentPhotoPath) continue;

                        if (ImageWatermarks.TryGetValue(photoPath, out var targetLayers))
                        {
                            var copy = targetLayers.FirstOrDefault(l =>
                                l.Id == targetId &&
                                l.IsGlobal &&
                                l.OriginalPhotoPath == targetOriginalPath);

                            if (copy != null)
                            {
                                targetLayers.Remove(copy);
                            }

                            SortAndReorderLayers(targetLayers);
                        }
                    }
                }
            }

            RefreshWatermarkComboBox();
            RenderPreviewWatermarks();
        }

        private void RefreshWatermarkComboBox()
        {
            bool prevUpdating = _isUpdatingUI;
            _isUpdatingUI = true;

            int savedSelectedIndex = MyComboBoxN1.SelectedIndex;
            MyComboBoxN1.Items.Clear();

            var layers = GetCurrentPhotoLayers();

            if (layers != null)
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    MyComboBoxN1.Items.Add(
                        new ComboBoxItem { Content = $"Водяний знак №{layers[i].Id}" });
                }

                if (savedSelectedIndex >= 0 && savedSelectedIndex < layers.Count)
                    MyComboBoxN1.SelectedIndex = savedSelectedIndex;
                else if (MyComboBoxN1.Items.Count > 0)
                    MyComboBoxN1.SelectedIndex = 0;
                else
                    MyComboBoxN1.SelectedIndex = -1;
            }

            _isUpdatingUI = prevUpdating;
            SyncUIWithSelectedWatermark();
            UpdateWatermarkUIFromSelection();
        }

        private void RenderPreviewWatermarks()
        {
            var imagesToRemove = PreviewCanvas.Children.OfType<Image>().ToList();
            foreach (var img in imagesToRemove)
            {
                PreviewCanvas.Children.Remove(img);
            }

            if (CurrentBitmap == null) return;

            if (PreviewRectangle != null)
            {
                Panel.SetZIndex(PreviewRectangle, 999);
            }

            if (SquareBorder.ActualWidth == 0 || SquareBorder.ActualHeight == 0) return;

            double scaleX = SquareBorder.ActualWidth / CurrentBitmap.PixelWidth;
            double scaleY = SquareBorder.ActualHeight / CurrentBitmap.PixelHeight;
            double uniformScale = Math.Min(scaleX, scaleY);

            double centerX = SquareBorder.ActualWidth / 2.0;
            double centerY = SquareBorder.ActualHeight / 2.0;

            PreviewScale = uniformScale;
            PreviewCenterX = centerX;
            PreviewCenterY = centerY;

            foreach (var layer in GetCurrentPhotoLayers())
            {
                double screenW = layer.Width * uniformScale;
                double screenH = layer.Height * uniformScale;

                Image wmImage = new Image
                {
                    Source = new BitmapImage(new Uri(layer.ImagePath, UriKind.RelativeOrAbsolute)),
                    Width = screenW,
                    Height = screenH,
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false,
                    Opacity = layer.Opacity
                };

                double screenX = centerX + (layer.X * uniformScale) - (screenW / 2.0);
                double screenY = centerY + (layer.Y * uniformScale) - (screenH / 2.0);

                Canvas.SetLeft(wmImage, screenX);
                Canvas.SetTop(wmImage, screenY);

                PreviewCanvas.Children.Add(wmImage);
            }
        }

        private void ToggleGlobalWatermark_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI) return;

            var currentLayers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;

            if (currentLayers != null && selectedIndex >= 0 && selectedIndex < currentLayers.Count)
            {
                var activeLayer = currentLayers[selectedIndex];

                activeLayer.IsGlobal = true;
                activeLayer.GlobalOrder++;

                int targetId = activeLayer.Id;
                string currentPhotoPath = GetCurrentPhotoPath();
                var allPhotos = GetAllImageFiles();

                if (allPhotos != null && !string.IsNullOrEmpty(currentPhotoPath))
                {
                    foreach (var photoPath in allPhotos)
                    {
                        if (photoPath == currentPhotoPath)
                            continue;

                        if (!ImageWatermarks.TryGetValue(photoPath, out var targetLayers))
                        {
                            targetLayers = new List<WatermarkLayer>();
                            ImageWatermarks[photoPath] = targetLayers;
                        }

                        var conflictingLocal = targetLayers.FirstOrDefault(l => l.Id == targetId);

                        if (conflictingLocal != null)
                        {
                            conflictingLocal.Id = GenerateNextAvailableLocalId(targetLayers);
                        }

                        var clonedLayer = new WatermarkLayer
                        {
                            Id = targetId,
                            ImagePath = activeLayer.ImagePath,
                            X = activeLayer.X,
                            Y = activeLayer.Y,
                            Width = activeLayer.Width,
                            Height = activeLayer.Height,
                            Opacity = activeLayer.Opacity,
                            OriginalPhotoPath = activeLayer.OriginalPhotoPath,
                            IsGlobal = true,
                            GlobalOrder = activeLayer.GlobalOrder,
                            IsAlignmentEnabled = activeLayer.IsAlignmentEnabled,
                            SelectedAlignment = activeLayer.SelectedAlignment,
                            UseAlignment = activeLayer.UseAlignment,
                            AlignmentIndex = activeLayer.AlignmentIndex
                        };

                        targetLayers.Add(clonedLayer);

                        SortAndReorderLayers(targetLayers);
                    }
                }

                SortAndReorderLayers(currentLayers);
                RefreshWatermarkComboBox();

                int newIndex = currentLayers.FindIndex(l => l.Id == targetId);
                if (newIndex >= 0) MyComboBoxN1.SelectedIndex = newIndex;
            }
        }

        private void ToggleGlobalWatermark_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI) return;

            var currentLayers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;

            if (currentLayers != null && selectedIndex >= 0 && selectedIndex < currentLayers.Count)
            {
                var activeLayer = currentLayers[selectedIndex];

                if (!activeLayer.IsGlobal) return;

                string originPath = !string.IsNullOrEmpty(activeLayer.OriginalPhotoPath)
                    ? activeLayer.OriginalPhotoPath
                    : GetCurrentPhotoPath();

                int targetGlobalOrder = activeLayer.GlobalOrder;

                var allPhotos = GetAllImageFiles();

                if (allPhotos != null)
                {
                    foreach (var photoPath in allPhotos)
                    {
                        if (string.IsNullOrEmpty(photoPath)) continue;

                        if (photoPath == originPath) continue;

                        if (!ImageWatermarks.TryGetValue(photoPath, out var targetLayers)) continue;

                        var copy = targetLayers.FirstOrDefault(l =>
                            l.IsGlobal && l.GlobalOrder == targetGlobalOrder && l.OriginalPhotoPath == originPath);

                        if (copy != null)
                        {
                            targetLayers.Remove(copy);
                        }

                        SortAndReorderLayers(targetLayers);
                    }
                }

                activeLayer.IsGlobal = false;
                activeLayer.GlobalOrder = 0;
                activeLayer.Id = GenerateNextAvailableLocalId(currentLayers);

                SortAndReorderLayers(currentLayers);
                RefreshWatermarkComboBox();
                SyncUIWithSelectedWatermark();
                UpdateWatermarkUIFromSelection();
                NotifyWatermarksChanged();
                RenderPreviewWatermarks();
            }
        }

        private void UpdateToggleApplyModeAvailability()
        {
            if (ToggleGlobalWatermark == null || ComboWatermarkScope == null || OneSize == null || _isUpdatingUI || _isInitializing) return;

            bool isGlobalOn = ToggleGlobalWatermark.IsChecked == true;
            bool isAllWatermarksSelected = ComboWatermarkScope.SelectedIndex == 1;
            bool isThisWatermarkSelected = ComboWatermarkScope.SelectedIndex == 0;

            if (isAllWatermarksSelected || (isThisWatermarkSelected && isGlobalOn))
            {
                OneSize.IsEnabled = true;
            }
            else
            {
                _isUpdatingUI = true;

                OneSize.IsChecked = false;
                OneSize.Content = "OFF";
                OneSize.IsEnabled = false;

                _isUpdatingUI = false;
            }
        }

        private void ComboWatermarkScope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboWatermarkScope == null) return;
            if (_isUpdatingUI || _isInitializing) return;

            UpdateToggleApplyModeAvailability();
            IsAllWatermarksMode = ComboWatermarkScope.SelectedIndex == 1;

            if (OneSize != null && OneSize.IsChecked == true)
            {
                SyncWatermarkSizes();
                UpdateToggleApplyModeAvailability();
            }
        }

        private void OneSize_Click(object sender, RoutedEventArgs e)
        {
            IsOneSizeOn = OneSize.IsChecked == true;
            OneSize.Content = IsOneSizeOn ? "ON" : "OFF";

            if (_isUpdatingUI || _isInitializing) return;

            if (OneSize.IsChecked == true)
            {
                OneSize.Content = "ON";
                SyncWatermarkSizes();
            }
            else
            {
                OneSize.Content = "OFF";
            }
        }

        private void SyncWatermarkSizes()
        {
            if (OneSize.IsChecked != true || _isUpdatingUI || _isInitializing) return;

            var currentLayers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;

            if (selectedIndex == -1 || selectedIndex >= currentLayers.Count) return;

            var baseLayer = currentLayers[selectedIndex];
            double targetWidth = baseLayer.Width;
            double targetHeight = baseLayer.Height;

            var allPhotos = GetAllImageFiles();
            bool isAllWatermarksMode = ComboWatermarkScope.SelectedIndex == 1;

            _isUpdatingUI = true;

            foreach (var photoPath in allPhotos)
            {
                if (!ImageWatermarks.TryGetValue(photoPath, out var targetLayers)) continue;

                foreach (var layer in targetLayers)
                {
                    if (isAllWatermarksMode)
                    {
                        layer.Width = targetWidth;
                        layer.Height = targetHeight;
                    }
                    else
                    {
                        if (layer.Id == baseLayer.Id)
                        {
                            layer.Width = targetWidth;
                            layer.Height = targetHeight;
                        }
                    }
                }
            }

            _isUpdatingUI = false;

            RenderPreviewWatermarks();
        }

        public void LoadCurrentWatermarkToUI()
        {
            if (_isUpdatingUI || _isInitializing || MyComboBoxN1 == null || WmWidthBox == null || WmHeightBox == null) return;

            var layers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;

            if (selectedIndex != -1 && selectedIndex < layers.Count)
            {
                var activeLayer = layers[selectedIndex];

                _isInitializing = true;
                WmWidthBox.Text = ((int)activeLayer.Width).ToString();
                WmHeightBox.Text = ((int)activeLayer.Height).ToString();

                WmXBox.Text = ((int)activeLayer.X).ToString();
                WmYBox.Text = ((int)activeLayer.Y).ToString();
                _isInitializing = false;
            }
        }

        private void WmTextBoxes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _isInitializing) return;

            if (sender is TextBox textBox)
            {
                string cleanText = System.Text.RegularExpressions.Regex.Replace(textBox.Text, "[^0-9]", "");
                if (textBox.Text != cleanText)
                {
                    int caret = textBox.CaretIndex;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = caret > 0 ? caret - 1 : 0;
                    return;
                }

                if (string.IsNullOrEmpty(cleanText) || cleanText == "0") return;

                if (int.TryParse(cleanText, out int val))
                {
                    var layers = GetCurrentPhotoLayers();
                    int selectedIndex = MyComboBoxN1.SelectedIndex;

                    if (selectedIndex != -1 && selectedIndex < layers.Count)
                    {
                        var activeLayer = layers[selectedIndex];

                        if (textBox.Name == "WmWidthBox") activeLayer.Width = val;
                        if (textBox.Name == "WmHeightBox") activeLayer.Height = val;



                        SyncWatermarkSizes();

                        if (IsWatermarkAlignmentEnabled)
                        {
                            UpdateAndTriggerAlignment();
                        }
                        RenderPreviewWatermarks();
                    }
                }
            }
        }

        private void AdjustWmValue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string[] parts = btn.Tag.ToString().Split('_');
                if (parts.Length == 2)
                {
                    string targetBoxName = parts[0];
                    string action = parts[1];

                    TextBox targetBox = null;
                    if (targetBoxName == "WmHeightBox") targetBox = WmHeightBox;
                    else if (targetBoxName == "WmWidthBox") targetBox = WmWidthBox;
                    else if (targetBoxName == "WmXBox") targetBox = WmXBox;
                    else if (targetBoxName == "WmYBox") targetBox = WmYBox;

                    if (targetBox != null && int.TryParse(targetBox.Text, out int currentValue))
                    {
                        if (action == "Up")
                        {
                            currentValue++;
                        }
                        else if (action == "Down")
                        {
                            if ((targetBoxName == "WmHeightBox" || targetBoxName == "WmWidthBox") && currentValue <= 5)
                                return;

                            currentValue--;
                        }

                        targetBox.Text = currentValue.ToString();
                    }
                }
            }
        }

        private void WmPos_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _isInitializing) return;

            if (IsWatermarkAlignmentEnabled)
            {
                UpdateCoordinateTextBoxes();
                return;
            }

            var layers = GetCurrentPhotoLayers();
            if (layers == null || MyComboBoxN1.SelectedIndex < 0 || MyComboBoxN1.SelectedIndex >= layers.Count)
                return;

            var layer = layers[MyComboBoxN1.SelectedIndex];

            bool parsedX = double.TryParse(WmXBox.Text, out double x);
            bool parsedY = double.TryParse(WmYBox.Text, out double y);

            if (parsedX && parsedY)
            {
                layer.X = x;
                layer.Y = y;

                if (IsSameIdAlignmentOn)
                {
                    SyncWatermarkPositions(layer);
                }

                RenderPreviewWatermarks();
            }
        }
        private void OnlyNumbers_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool isNumber = System.Text.RegularExpressions.Regex.IsMatch(e.Text, "[0-9.-]");
            e.Handled = !isNumber;
        }

        private void ToggleSameIdAlignment_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;

            ToggleSameIdAlignment.Content = "ON";

            var layers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;
            if (layers == null || selectedIndex == -1 || selectedIndex >= layers.Count) return;

            var activeLayer = layers[selectedIndex];

            if (!activeLayer.IsGlobal) return;

            string currentFile = activeLayer.OriginalPhotoPath;

            double sourceX = activeLayer.X;
            double sourceY = activeLayer.Y;

            foreach (var photoPath in ImageWatermarks.Keys)
            {
                if (photoPath == currentFile) continue;

                var sameLayer = ImageWatermarks[photoPath]
                    .FirstOrDefault(l => l.IsGlobal && l.GlobalOrder == activeLayer.GlobalOrder);

                if (sameLayer != null)
                {
                    sameLayer.X = sourceX;
                    sameLayer.Y = sourceY;

                    ApplyWatermarkAlignment(sameLayer, isDragging: false, photoPath: photoPath);
                }
            }

            NotifyWatermarksChanged();
            RenderPreviewWatermarks();
        }

        private void ToggleSameIdAlignment_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ToggleSameIdAlignment != null) ToggleSameIdAlignment.Content = "OFF";
            IsSameIdAlignmentOn = false;
        }

        private void WMToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingUI) return;

            var layers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;
            if (layers == null || selectedIndex == -1 || selectedIndex >= layers.Count) return;

            var activeLayer = layers[selectedIndex];
            bool isChecked = WMToggleBtn.IsChecked ?? false;

            WMToggleBtn.Content = isChecked ? "ON" : "OFF";
            WMAlignmentComboBox.IsEnabled = isChecked;

            activeLayer.UseAlignment = isChecked;
            if (isChecked)
            {
                activeLayer.AlignmentIndex = WMAlignmentComboBox.SelectedIndex;
            }

            if (activeLayer.IsGlobal)
            {
                SyncGlobalAlignmentSettings(activeLayer);
            }
            else
            {
                ApplyWatermarkAlignment(activeLayer, isDragging: false);
            }

            NotifyWatermarksChanged();
            RenderPreviewWatermarks();
        }
        
        private void SyncGlobalAlignmentSettings(WatermarkLayer activeLayer)
        {
            var allPhotos = GetAllImageFiles();
            if (allPhotos == null) return;

            foreach (var photoPath in allPhotos)
            {
                if (ImageWatermarks.TryGetValue(photoPath, out var targetLayers))
                {
                    var copy = targetLayers.FirstOrDefault(l => l.Id == activeLayer.Id && l.IsGlobal && l.OriginalPhotoPath == activeLayer.OriginalPhotoPath);

                    if (copy != null)
                    {
                        copy.UseAlignment = activeLayer.UseAlignment;
                        copy.AlignmentIndex = activeLayer.AlignmentIndex;

                        ApplyWatermarkAlignment(copy, isDragging: false, photoPath: photoPath);
                    }
                }
            }
        }

        public void UpdateCoordinateTextBoxes()
        {
            var layers = GetCurrentPhotoLayers();
            if (layers == null || MyComboBoxN1.SelectedIndex < 0 || MyComboBoxN1.SelectedIndex >= layers.Count)
                return;

            var layer = layers[MyComboBoxN1.SelectedIndex];

            _isUpdatingUI = true;

            try
            {
                if (WmXBox != null) WmXBox.Text = layer.X.ToString("F0");
                if (WmYBox != null) WmYBox.Text = layer.Y.ToString("F0");

                if (WmOpacitySlider != null)
                {
                    WmOpacitySlider.Value = layer.Opacity * 100.0;
                }
                if (WmOpacityLabel != null)
                {
                    WmOpacityLabel.Text = $"{Math.Round(layer.Opacity * 100.0)}%";
                }
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void WmOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUI || _isInitializing) return;

            if (WmOpacityLabel != null)
            {
                WmOpacityLabel.Text = $"{Math.Round(e.NewValue)}%";
            }

            var layers = GetCurrentPhotoLayers();
            if (layers == null || MyComboBoxN1.SelectedIndex < 0 || MyComboBoxN1.SelectedIndex >= layers.Count)
                return;

            var layer = layers[MyComboBoxN1.SelectedIndex];
            double targetOpacity = e.NewValue / 100.0;
            layer.Opacity = targetOpacity;

            if (ToggleOpacityScope != null && ToggleOpacityScope.IsChecked == true)
            {
                if (IsAllWatermarksOpacityMode)
                {
                    SyncAllWatermarksOpacity(targetOpacity);
                }
                else
                {
                    SyncWatermarkOpacityById(layer);
                }
            }

            RenderPreviewWatermarks();
        }

        private void OpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (WmOpacitySlider != null)
            {
                WmOpacitySlider.Value = Math.Max(WmOpacitySlider.Minimum, WmOpacitySlider.Value - 1);
            }
        }

        private void OpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (WmOpacitySlider != null)
            {
                WmOpacitySlider.Value = Math.Min(WmOpacitySlider.Maximum, WmOpacitySlider.Value + 1);
            }
        }

        private void UpdateOpacityScopeAvailability()
        {
            if (ComboOpacityScope == null || ToggleOpacityScope == null || ToggleGlobalWatermark == null) return;

            if (ComboOpacityScope.SelectedIndex == 1)
            {
                ToggleOpacityScope.IsEnabled = true;
            }
            else
            {
                if (ToggleGlobalWatermark.IsChecked == true)
                {
                    ToggleOpacityScope.IsEnabled = true;
                }
                else
                {
                    bool previouslyUpdating = _isUpdatingUI;
                    _isUpdatingUI = true;

                    ToggleOpacityScope.IsChecked = false;
                    ToggleOpacityScope.Content = "OFF";
                    ToggleOpacityScope.IsEnabled = false;

                    _isUpdatingUI = previouslyUpdating;
                }
            }
        }

        private void ComboOpacityScope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI || _isInitializing) return;

            bool isAllMode = (ComboOpacityScope.SelectedIndex == 1);
            IsAllWatermarksOpacityMode = isAllMode;

            UpdateOpacityScopeAvailability();

            if (ToggleOpacityScope != null && ToggleOpacityScope.IsChecked == true && WmOpacitySlider != null)
            {
                var layers = GetCurrentPhotoLayers();
                if (layers != null && MyComboBoxN1.SelectedIndex >= 0 && MyComboBoxN1.SelectedIndex < layers.Count)
                {
                    var layer = layers[MyComboBoxN1.SelectedIndex];
                    double currentOpacity = WmOpacitySlider.Value / 100.0;

                    if (isAllMode)
                    {
                        SyncAllWatermarksOpacity(currentOpacity);
                    }
                    else
                    {
                        SyncWatermarkOpacityById(layer);
                    }

                    RenderPreviewWatermarks();
                }
            }
        }

        private void ToggleOpacityScope_Click(object sender, RoutedEventArgs e)
        {
            if (ToggleOpacityScope == null) return;

            bool isChecked = ToggleOpacityScope.IsChecked ?? false;
            ToggleOpacityScope.Content = isChecked ? "ON" : "OFF";

            if (WmOpacitySlider != null)
            {
                var args = new RoutedPropertyChangedEventArgs<double>(WmOpacitySlider.Value, WmOpacitySlider.Value);
                WmOpacitySlider_ValueChanged(WmOpacitySlider, args);
            }
        }

        private void SyncUIWithSelectedWatermark()
        {
            if (_isInitializing) return;

            _isUpdatingUI = true;

            try
            {
                int selectedIndex = MyComboBoxN1.SelectedIndex;
                var currentLayers = GetCurrentPhotoLayers();

                if (selectedIndex == -1 || currentLayers == null || selectedIndex >= currentLayers.Count)
                {
                    ToggleGlobalWatermark.IsChecked = false;
                    ToggleGlobalWatermark.Content = "OFF";
                    return;
                }

                var activeLayer = currentLayers[selectedIndex];

                bool isGloballyCopied = false;

                var allPhotos = GetAllImageFiles();
                string currentPhotoPath = GetCurrentPhotoPath();

                if (allPhotos != null)
                {
                    foreach (var photoPath in allPhotos)
                    {
                        if (photoPath == currentPhotoPath)
                            continue;

                        if (ImageWatermarks.TryGetValue(photoPath, out var targetLayers))
                        {
                            if (targetLayers.Any(l =>
                                l.Id == activeLayer.Id &&
                                l.IsGlobal))
                            {
                                isGloballyCopied = true;
                                break;
                            }
                        }
                    }
                }

                ToggleGlobalWatermark.IsChecked = isGloballyCopied;
                ToggleGlobalWatermark.Content = isGloballyCopied ? "ON" : "OFF";
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }
        private void SortAndReorderLayers(List<ImageService.WatermarkLayer> layers)
        {
            if (layers == null || layers.Count <= 1) return;

            var sorted = layers
                .OrderBy(x => x.IsGlobal ? 0 : 1)
                .ThenBy(x => x.IsGlobal ? x.GlobalOrder : 0)
                .ThenBy(x => x.Id)
                .ToList();

            layers.Clear();
            layers.AddRange(sorted);
        }
        private int GenerateNextAvailableLocalId(List<ImageService.WatermarkLayer> layers, ImageService.WatermarkLayer currentLayer = null)
        {
            int candidateId = 1;
            if (layers != null)
            {
                while (layers.Any(l => (l.Id - 1) == candidateId && (currentLayer == null || l != currentLayer)))
                {
                    candidateId++;
                }
            }
            return candidateId;
        }

        private void UpdateWatermarkUIFromSelection()
        {
            var layers = GetCurrentPhotoLayers();
            int selectedIndex = MyComboBoxN1.SelectedIndex;

            if (layers == null || selectedIndex == -1 || selectedIndex >= layers.Count)
            {
                _isUpdatingUI = true;
                WatermarkPreviewImage.Source = null;
                WMToggleBtn.IsChecked = false;
                WMToggleBtn.Content = "OFF";
                WMAlignmentComboBox.IsEnabled = false;
                WMAlignmentComboBox.SelectedIndex = 0;
                _isUpdatingUI = false;
                return;
            }

            var activeLayer = layers[selectedIndex];

            LoadCurrentWatermarkToUI();
            UpdateCoordinateTextBoxes();
            UpdateOpacityScopeAvailability();
            UpdateToggleApplyModeAvailability();



            if (!string.IsNullOrEmpty(activeLayer.ImagePath) && System.IO.File.Exists(activeLayer.ImagePath))
            {
                try
                {
                    WatermarkPreviewImage.Source = new BitmapImage(new Uri(activeLayer.ImagePath, UriKind.RelativeOrAbsolute));
                }
                catch
                {
                }
            }

            _isUpdatingUI = true;
            try
            {
                WMToggleBtn.IsChecked = activeLayer.UseAlignment;
                WMToggleBtn.Content = activeLayer.UseAlignment ? "ON" : "OFF";
                WMAlignmentComboBox.IsEnabled = activeLayer.UseAlignment;
                WMAlignmentComboBox.SelectedIndex = activeLayer.AlignmentIndex;
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void LoadWatermarkProfiles()
        {
            MyComboBoxN3Watermark.Items.Clear();
            foreach (var p in WatermarkProfileService.Profiles)
                MyComboBoxN3Watermark.Items.Add(p.Name);
        }

        private void ApplyToAllWatermark_Checked(object sender, RoutedEventArgs e)
        {
            ApplyToAllWatermark.Content = "Для всіх полів";
        }

        private void ApplyToAllWatermark_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyToAllWatermark.Content = "Для цього поля";
        }

        private void SaveProfileWatermark_Click(object sender, RoutedEventArgs e)
        {
            string profileName = MyComboBoxN3Watermark.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(profileName))
            {
                MessageBox.Show("Введіть назву профілю");
                return;
            }

            var layers = GetCurrentPhotoLayers();
            var snapshots = layers.Select(l => new WatermarkLayerSnapshot
            {
                ImagePath    = l.ImagePath,
                X            = l.X,
                Y            = l.Y,
                Width        = l.Width,
                Height       = l.Height,
                Opacity      = l.Opacity,
                UseAlignment = l.UseAlignment,
                AlignmentIndex = l.AlignmentIndex
            }).ToList();

            var existing = WatermarkProfileService.Profiles.FirstOrDefault(p => p.Name == profileName);
            if (existing != null) WatermarkProfileService.Profiles.Remove(existing);

            var profile = new WatermarkProfile
            {
                Name       = profileName,
                ApplyToAll = ApplyToAllWatermark.IsChecked == true,
                Layers     = snapshots
            };
            WatermarkProfileService.Profiles.Add(profile);
            WatermarkProfileService.Save();

            string currentPath = GetCurrentPhotoPath();
            if (!string.IsNullOrEmpty(currentPath))
                _wmPhotoProfiles[currentPath] = profileName;

            LoadWatermarkProfiles();

            _isUpdatingUI = true;

            MyComboBoxN3Watermark.SelectedItem = profileName;
            if (MyComboBoxN3Watermark.SelectedIndex < 0) MyComboBoxN3Watermark.Text = profileName;
            _isUpdatingUI = false;
        }

        private void MyComboBoxN3Watermark_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI) return;
            if (MyComboBoxN3Watermark.SelectedItem == null) return;

            string selected = MyComboBoxN3Watermark.SelectedItem.ToString() ?? string.Empty;
            var profile = WatermarkProfileService.Profiles.FirstOrDefault(p => p.Name == selected);
            if (profile == null) return;

            if (ApplyToAllWatermark.IsChecked == true)
            {
                foreach (var path in GetAllImageFiles())
                    _wmPhotoProfiles[path] = selected;
            }
            else
            {
                string photoPath = GetCurrentPhotoPath();
                if (!string.IsNullOrEmpty(photoPath)) _wmPhotoProfiles[photoPath] = selected;
            }

            ApplyToAllWatermark.IsChecked = profile.ApplyToAll;
            ApplyWatermarkProfileByName(selected);
        }

        private void DeleteProfileWatermark_Click(object sender, RoutedEventArgs e)
        {
            if (MyComboBoxN3Watermark.SelectedItem == null) return;

            string selected = MyComboBoxN3Watermark.SelectedItem.ToString() ?? string.Empty;
            var profile = WatermarkProfileService.Profiles.FirstOrDefault(p => p.Name == selected);
            if (profile == null) return;

            WatermarkProfileService.Profiles.Remove(profile);
            WatermarkProfileService.Save();

            var toRemove = _wmPhotoProfiles.Where(kv => kv.Value == selected).Select(kv => kv.Key).ToList();
            foreach (var key in toRemove) _wmPhotoProfiles.Remove(key);

            LoadWatermarkProfiles();
            MyComboBoxN3Watermark.SelectedIndex = -1;
            MyComboBoxN3Watermark.Text = string.Empty;
        }

        private void ApplyWatermarkProfileByName(string profileName)
        {
            var profile = WatermarkProfileService.Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null) return;

            string currentPath = GetCurrentPhotoPath();
            if (string.IsNullOrEmpty(currentPath)) return;

            var layers = GetCurrentPhotoLayers();
            layers.RemoveAll(l => !l.IsGlobal);

            int nextId = 1;
            foreach (var snap in profile.Layers)
            {
                while (layers.Any(l => l.Id == nextId)) nextId++;

                var layer = new WatermarkLayer
                {
                    Id             = nextId++,
                    ImagePath      = snap.ImagePath,
                    X              = snap.X,
                    Y              = snap.Y,
                    Width          = snap.Width,
                    Height         = snap.Height,
                    Opacity        = snap.Opacity,
                    UseAlignment   = snap.UseAlignment,
                    AlignmentIndex = snap.AlignmentIndex,
                    IsGlobal       = false,
                    OriginalPhotoPath = currentPath
                };
                layers.Add(layer);
                ApplyWatermarkAlignment(layer, isDragging: false);
            }

            NotifyWatermarksChanged();
            RefreshWatermarkComboBox();
            RenderPreviewWatermarks();
        }

        private void streams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (streams.SelectedItem != null)
            {
                int threadCount = Convert.ToInt32(streams.SelectedItem);

                Properties.Settings.Default.ThreadCount = threadCount;
                Properties.Settings.Default.Save();
            }
        }

        private void RedactorName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.RedactorName = RedactorName.Text;

            Properties.Settings.Default.Save();
        }


        public void UpdatePermissions()
        {
            bool isAdmin = AuthService.CurrentUser?.Role == "Admin";
            WatermarkToggle.IsEnabled = isAdmin;
            AdminUsersPanel.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (isAdmin)
            LoadUsersComboBox();
        }


        private void LoadUsersComboBox()
        {
            Users.Items.Clear();
            foreach (var u in AuthService.Users)
                Users.Items.Add(u.Login);

            if (Users.Items.Count > 0) Users.SelectedIndex = 0;
        }

        private void Users_Changed(object sender, SelectionChangedEventArgs e) { }

        private void ButtonRemoveUsers_Click(object sender, RoutedEventArgs e)
        {
            if (Users.SelectedItem is not string login) return;

            if (login == AuthService.CurrentUser?.Login)
            {
                MessageBox.Show("Не можна видалити самого себе.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Видалити користувача \"{login}\"?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var user = AuthService.Users.FirstOrDefault(u => u.Login == login);
            if (user != null)
            {
                AuthService.Users.Remove(user);
                AuthService.Save();
                LoadUsersComboBox();
            }
        }

        private void ButtonChangeUsersPassword_Click(object sender, RoutedEventArgs e)
        {
            if (Users.SelectedItem is not string login) return;

            var dialog = new Pages.UserEditDialog(login, Pages.UserEditMode.Repassword)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true) return;

            var user = AuthService.Users.FirstOrDefault(u => u.Login == login);
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dialog.ResultValue);
                AuthService.Save();
                MessageBox.Show($"Пароль для \"{login}\" змінено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButtonRenameUsers_Click(object sender, RoutedEventArgs e)
        {
            if (Users.SelectedItem is not string login) return;

            var dialog = new Pages.UserEditDialog(login, Pages.UserEditMode.Rename)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true) return;

            string newName = dialog.ResultValue.Trim();

            if (AuthService.Users.Any(u => u.Login == newName))
            {
                MessageBox.Show("Користувач з таким іменем вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = AuthService.Users.FirstOrDefault(u => u.Login == login);
            if (user != null)
            {
                if (AuthService.CurrentUser?.Login == login)
                    AuthService.CurrentUser.Login = newName;

                user.Login = newName;
                AuthService.Save();
                LoadUsersComboBox();
                MessageBox.Show($"Користувача перейменовано на \"{newName}\".", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void UsersNewName_TextChanged(object sender, TextChangedEventArgs e) { }
        private void UsersNewPassword_TextChanged(object sender, RoutedEventArgs e) { }


        private void SelectChanges_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Ви не ввійшли в систему.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newName = UsersNewName.Text.Trim();
            string newPassword = UsersNewPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(newName) && string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Введіть нове ім'я та/або пароль.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmDlg = new ImageProcessing.Pages.ConfirmIdentityDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (confirmDlg.ShowDialog() != true) return;

            var user = AuthService.CurrentUser;

            if (!string.IsNullOrWhiteSpace(newName) && newName != user.Login)
            {
                if (AuthService.Users.Any(u => u.Login == newName))
                {
                    MessageBox.Show("Ім'я вже зайняте.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                user.Login = newName;
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            AuthService.Save();

            MessageBox.Show("Дані акаунту оновлено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

            UsersNewName.Text = string.Empty;
            UsersNewPassword.Password = string.Empty;
        }

        private void OneAlignmentCheck_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
