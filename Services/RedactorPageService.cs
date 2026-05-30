using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageProcessing.Services
{
    public class RedactorPageService
    {
        public static string SelectFolder(object sender, RoutedEventArgs e, string folder)
        {
            if (sender is not Button clickedButton)
            {
                if (sender is not TextBox clickedTextBox)
                {
                    throw new Exception("Щось пішло не так :/");
                }
                else
                {
                    string mode = "";
                    switch (clickedTextBox.Name)
                    {
                        case "M1FolderEnter":
                            mode = "enter";
                            break;
                        case "M1FolderOut":
                            mode = "out";
                            break;
                        case "M2WatermarkPath":
                            mode = "watermark";
                            break;
                        default: throw new Exception("Щось пішло не так :/");
                    }

                    if (!Directory.Exists(folder))
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"Шлях до папки не знайдено:\n{folder}\n\nСтворити відсутні папки?",
                            "Помилка шляху",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                Directory.CreateDirectory(folder);
                                MessageBox.Show("Папки успішно створені!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не вдалося створити папку: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            return "reset";
                        }
                    }
                    else
                    {
                        if (mode == "enter")
                        {
                            Properties.Settings.Default.LastSelectedPathEnter = folder;
                        }
                        else if (mode == "out")
                        {
                            Properties.Settings.Default.LastSelectedPathOut = folder;
                        }
                        else if (mode == "watermark")
                        {
                            Properties.Settings.Default.WatermarkFilePath = folder;
                        }
                        Properties.Settings.Default.Save();
                    }
                }
            }
            else
            {
                string? savedPath = null;
                string mode = "";

                switch (clickedButton.Name)
                {
                    case "ButtonEnter":
                        savedPath = Properties.Settings.Default.LastSelectedPathEnter; mode = "enter";
                        break;
                    case "ButtonOut":
                        savedPath = Properties.Settings.Default.LastSelectedPathOut; mode = "out";
                        break;
                    case "ButtonWatermarkBrowse":
                        savedPath = Properties.Settings.Default.WatermarkFilePath; mode = "watermark";
                        break;

                    default: throw new Exception("Щось пішло не так :/");
                }

                OpenFolderDialog dialog = new OpenFolderDialog
                {
                    Title = "Оберіть папку",
                };

                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                {
                    dialog.InitialDirectory = savedPath;
                }
                else
                {
                    dialog.InitialDirectory = "shell:MyComputerFolder";
                }

                if (dialog.ShowDialog() == true)
                {
                    string currentPath = dialog.FolderName;

                    MessageBox.Show($"Обрано та збережено: {currentPath}");

                    if (mode == "enter")
                    {
                        Properties.Settings.Default.LastSelectedPathEnter = currentPath;
                    }
                    else if (mode == "out")
                    {
                        Properties.Settings.Default.LastSelectedPathOut = currentPath;
                    }
                    else if (mode == "watermark")
                    {
                        Properties.Settings.Default.WatermarkFilePath = currentPath;
                    }

                    Properties.Settings.Default.Save();
                    return currentPath;
                }
            }
            return string.Empty;
        }

        public static double ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sliderVal = e.NewValue;

            double resultZoom;
            double returnedzoom;

            if (sliderVal <= 50)

            {
                double[] zoomLevels = { 12.5, 25, 50 };

                resultZoom = 12.5 + sliderVal / 50 * (100 - 12.5);

                returnedzoom = zoomLevels.OrderBy(x => Math.Abs(x - resultZoom)).First();
            }
            else
            {
                resultZoom = 100 + (sliderVal - 50) / 50 * (600 - 100);

                returnedzoom = Math.Round(resultZoom / 100.0) * 100;
            }
            return returnedzoom;
        }

        public static double ZoomButtons(object sender, double SliderValue, RoutedEventArgs e)
        {
            bool plus = false;

            if (SliderValue == 100) SliderValue = 101;

            if (sender is not Button clickedButton)
            {
                if (e is MouseWheelEventArgs mouseArgs)
                {
                    if (mouseArgs.Delta > 0)
                    {
                        plus = true;
                    }
                    else if (mouseArgs.Delta < 0)
                    {
                        plus = false;
                    }
                }
            }
            else
            {

                switch (clickedButton.Name)
                {
                    case "plus":
                        plus = true;
                        break;
                    case "minus":
                        plus = false;
                        break;
                    default: throw new Exception("Щось пішло не так :/");
                }
            }

            if (plus)
            {
                if (SliderValue >= 50)
                {
                    SliderValue += 10;
                }
                else if (SliderValue < 50)
                {
                    if (SliderValue == 0) SliderValue = 12.5;
                    else if (SliderValue == 12.5) SliderValue = 25;
                    else if (SliderValue == 25) SliderValue = 51;
                }
            }
            else
            {
                if (SliderValue > 51)
                {
                    SliderValue -= 10;
                }
                else
                {
                    if (SliderValue == 12.5) SliderValue = 0;
                    else if (SliderValue == 25) SliderValue = 12.5;
                    else if (SliderValue == 51) SliderValue =25;
                }
            }
            if (SliderValue >= 100) SliderValue = 101;
            if (SliderValue < 0) SliderValue = 0;
            return SliderValue;
        }

    }
}
