using ImageProcessing.Pages;
using ImageProcessing.Services;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using ImageMagick;

using Path = System.IO.Path;


namespace ImageProcessing
{
    public partial class MainWindow : Window
    {
        public Redactor _redactorPage;
        public Settings _settingsPage;

        public MainWindow()
        {
            InitializeComponent();
           
            _redactorPage = new Redactor();
            _settingsPage = new Settings(null);
            MainFrame.Navigate(new Login());
            
        }

        private void GoToMain_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(_redactorPage);
        }

        private void GoToSettings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(_settingsPage);
        }

        private void GoToHistory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new History());
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is not Redactor)
            {
                MessageBox.Show(
                    "Будь ласка, спочатку перейдіть на сторінку редактора.",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string inputFolder = Properties.Settings.Default.LastSelectedPathEnter;
            string outputFolder = Properties.Settings.Default.LastSelectedPathOut;

            if (string.IsNullOrEmpty(inputFolder) || !Directory.Exists(inputFolder))
            {
                MessageBox.Show(
                    "Вхідна папка не налаштована або не існує!",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show(
                    "Папка для збереження не вказана!",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Directory.CreateDirectory(outputFolder);

            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

            string[] files = Directory.GetFiles(inputFolder)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show(
                    "У вхідній папці немає фотографій.",
                    "Інформація",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string qualityString = ImageService.SelectedQuality?.ToLower() ?? "1080p";

            string format = ImageService.SelectedFormat?.ToUpper() ?? "PNG";

            CancellationTokenSource cts = new CancellationTokenSource();

            ProgressWindow progressWin = new ProgressWindow(cts, files.Length);

            progressWin.Owner = this;
            progressWin.Show();

            int processedCount = 0;

            try
            {
                int threads = Properties.Settings.Default.ThreadCount;

                await Task.Run(() =>
                {
                    ParallelOptions options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = threads,
                        CancellationToken = cts.Token
                    };

                    Parallel.ForEach(files, options, filePath =>
                    {
                        if (cts.Token.IsCancellationRequested) return;

                        try
                        {
                            using Bitmap originalBitmap = new Bitmap(filePath);

                            Int32Rect cropRect = ImageService.GetCropForFile(filePath);

                            if (cropRect == Int32Rect.Empty || cropRect.Width <= 0 || cropRect.Height <= 0)
                            {
                                cropRect = new Int32Rect(
                                    0,
                                    0,
                                    originalBitmap.Width,
                                    originalBitmap.Height);
                            }

                            int x = cropRect.X;
                            int y = cropRect.Y;
                            int width = cropRect.Width;
                            int height = cropRect.Height;

                            if (x < 0)
                            {
                                width += x;
                                x = 0;
                            }

                            if (y < 0)
                            {
                                height += y;
                                y = 0;
                            }

                            if (x >= originalBitmap.Width || y >= originalBitmap.Height)
                            {
                                return;
                            }

                            if (x + width > originalBitmap.Width)
                            {
                                width = originalBitmap.Width - x;
                            }

                            if (y + height > originalBitmap.Height)
                            {
                                height = originalBitmap.Height - y;
                            }

                            if (width <= 0 || height <= 0)
                            {
                                return;
                            }

                            using Bitmap croppedBitmap = new Bitmap(width, height);

                            using (Graphics g = Graphics.FromImage(croppedBitmap))
                            {
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                                g.DrawImage(
                                    originalBitmap,
                                    new Rectangle(0, 0, width, height),
                                    new Rectangle(x, y, width, height),
                                    GraphicsUnit.Pixel);

                                if (ImageService.ImageWatermarks.TryGetValue(filePath, out var layers) && layers != null)
                                {
                                    foreach (var wm in layers)
                                    {
                                        if (string.IsNullOrEmpty(wm.ImagePath) || !File.Exists(wm.ImagePath))
                                        {
                                            continue;
                                        }

                                        using FileStream fs = new FileStream(wm.ImagePath, FileMode.Open, FileAccess.Read);

                                        using Bitmap tempBitmap = new Bitmap(fs);

                                        using Bitmap wmBitmap = new Bitmap(tempBitmap);

                                        float opacityAlpha = (float)wm.Opacity;

                                        float[][] matrixItems =
                                        {
                                            new float[] {1,0,0,0,0},
                                            new float[] {0,1,0,0,0},
                                            new float[] {0,0,1,0,0},
                                            new float[] {0,0,0,opacityAlpha,0},
                                             new float[] {0,0,0,0,1}
                                        };

                                        ColorMatrix colorMatrix = new ColorMatrix(matrixItems);

                                        using ImageAttributes attributes = new ImageAttributes();

                                        attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                                        int wmWidth = (int)wm.Width;
                                        int wmHeight = (int)wm.Height;

                                        if (wmWidth <= 0 || wmHeight <= 0)
                                        {
                                            continue;
                                        }

                                        int wmX = (int)Math.Round(width / 2.0 + wm.X - wmWidth / 2.0);
                                        int wmY = (int)Math.Round(height / 2.0 + wm.Y - wmHeight / 2.0);

                                        g.DrawImage(wmBitmap, new System.Drawing.Rectangle(wmX, wmY, wmWidth, wmHeight), 0, 0, wmBitmap.Width, wmBitmap.Height, System.Drawing.GraphicsUnit.Pixel, attributes);
                                    }
                                }
                            }

                            double targetHeight = qualityString switch
                            {
                                var q when q.Contains("144p") => 144,
                                var q when q.Contains("240p") => 240,
                                var q when q.Contains("360p") => 360,
                                var q when q.Contains("480p") => 480,
                                var q when q.Contains("720p") => 720,
                                var q when q.Contains("1080p") => 1080,
                                var q when q.Contains("2к") || q.Contains("2k") => 1440,
                                var q when q.Contains("4к") || q.Contains("4k") => 2160,
                                _ => croppedBitmap.Height
                            };

                            Bitmap finalBitmap = croppedBitmap;
                            bool isResized = false;

                            if (Math.Abs(targetHeight - croppedBitmap.Height) > 1)
                            {
                                double scale = targetHeight / croppedBitmap.Height;

                                int finalWidth = (int)(croppedBitmap.Width * scale);

                                int finalHeight = (int)targetHeight;

                                Bitmap resizedBitmap = new Bitmap(finalWidth, finalHeight);

                                using (Graphics gResize = Graphics.FromImage(resizedBitmap))
                                {
                                    gResize.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                                    gResize.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                                    gResize.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                                    gResize.DrawImage(croppedBitmap, new System.Drawing.Rectangle(
                                            0,
                                            0,
                                            finalWidth,
                                            finalHeight),
                                        0,
                                        0,
                                        croppedBitmap.Width,
                                        croppedBitmap.Height,
                                        GraphicsUnit.Pixel);
                                }

                                finalBitmap = resizedBitmap;
                                isResized = true;
                            }

                            string ext = format switch
                            {
                                var f when f.Contains("JPEG") || f.Contains("JPG") => ".jpg",
                                var f when f.Contains("PNG") => ".png",
                                var f when f.Contains("BMP") => ".bmp",
                                var f when f.Contains("WEBP") => ".webp",
                                var f when f.Contains("AVIF") => ".avif",
                                _ => ".png"
                            };

                            string outPath = Path.Combine(outputFolder, Path.ChangeExtension(Path.GetFileName(filePath), ext));

                            bool useMagick = ext is ".webp" or ".avif";

                            if (useMagick)
                            {
                                using MemoryStream ms = new MemoryStream();
                                ImageFormat tmpFmt = ImageFormat.Png;
                                finalBitmap.Save(ms, tmpFmt);
                                ms.Position = 0;

                                MagickImage magickImg = new MagickImage(ms);

                                MagickFormat magickFormat = ext switch
                                {
                                    ".webp" => MagickFormat.WebP,
                                    ".avif" => MagickFormat.Avif,
                                    _ => MagickFormat.Png
                                };

                                magickImg.Format = magickFormat;
                                magickImg.Write(outPath);
                            }
                            else
                            {
                                ImageFormat saveFormat = ext switch
                                {
                                    ".jpg" => ImageFormat.Jpeg,
                                    ".bmp" => ImageFormat.Bmp,
                                    _ => ImageFormat.Png
                                };
                                finalBitmap.Save(outPath, saveFormat);
                            }

                            if (isResized)
                            {
                                finalBitmap.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(
                                    $"Файл:\n{filePath}\n\n{ex}",
                                    "ПОМИЛКА",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            });
                        }

                        Interlocked.Increment(ref processedCount);

                        Dispatcher.Invoke(() =>
                        {
                            progressWin.UpdateProgress(processedCount, files.Length);
                        });
                    });
                }, cts.Token);

                progressWin.Close();

                string taskName = string.IsNullOrWhiteSpace(Properties.Settings.Default.RedactorName) ? $"Num{HistoryService.Tasks.Count + 1}" : Properties.Settings.Default.RedactorName;

                HistoryService.AddTask(taskName, "Completed", files.Length);

                MessageBox.Show(
                    $"Готово! Всі {files.Length} фото обрізано.",
                    "Успіх",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                if (progressWin.IsVisible)
                    progressWin.Close();

                string taskName =
                    string.IsNullOrWhiteSpace(Properties.Settings.Default.RedactorName) ? $"Num{HistoryService.Tasks.Count + 1}" : Properties.Settings.Default.RedactorName;

                HistoryService.AddTask(taskName, "Canceled", processedCount);

                MessageBox.Show(
                    $"Процес було перервано.\nОброблено {processedCount} з {files.Length} фото.",
                    "Відміна",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                if (progressWin.IsVisible)
                    progressWin.Close();

                MessageBox.Show(
                    ex.ToString(),
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                cts.Dispose();
            }
        }

    }
}