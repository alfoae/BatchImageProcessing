using System.Threading;
using System.Windows;

namespace ImageProcessing
{
    public partial class ProgressWindow : Window
    {
        private CancellationTokenSource _cts;

        public ProgressWindow(CancellationTokenSource cts, int maxImages)
        {
            InitializeComponent();
            _cts = cts;
            MainProgressBar.Maximum = maxImages;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                CancelBtn.IsEnabled = false;
                CancelBtn.Content = "Зупиняємо...";
            }
        }
        public void UpdateProgress(int current, int total)
        {
            MainProgressBar.Value = current;
            StatusText.Text = $"Обробка: {current} з {total}";
        }
    }
}