using ImageProcessing.Models;
using ImageProcessing.Services;
using System.Windows;
using System.Windows.Controls;

namespace ImageProcessing.Pages
{
    public partial class History : Page
    {
        private int currentPage = 1;

        public History()
        {
            InitializeComponent();

            StatusFilter.SelectionChanged -= FilterChanged;
            SortBox.SelectionChanged -= FilterChanged;
            PageSizeBox.SelectionChanged -= FilterChanged;

            StatusFilter.SelectedIndex = 0;
            SortBox.SelectedIndex = 0;
            PageSizeBox.SelectedIndex = 0;

            StatusFilter.SelectionChanged += FilterChanged;
            SortBox.SelectionChanged += FilterChanged;
            PageSizeBox.SelectionChanged += FilterChanged;

            LoadData();
        }

        private void LoadData()
        {
            IEnumerable<TaskHistory> data =
                HistoryService.Tasks;

            string search =
                SearchBox.Text.ToLower();

            if (!string.IsNullOrWhiteSpace(search))
            {
                data = data.Where(x =>
                    x.TaskName.ToLower().Contains(search));
            }

            string status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (status == "Завершені")
            {
                status = "Completed";
            }
            else if (status == "Скасовані")
            {
                status = "Canceled";
            }

            if (status != "Усі")
            {
                data = data.Where(x =>
                    x.Status == status);
            }

            string sort = (SortBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            data = sort switch
            {
                "Спочатку нові" =>
                    data.OrderByDescending(x => x.Date),

                "Спочатку старі"  =>
                    data.OrderBy(x => x.Date),

                "Більше файлів" =>
                    data.OrderByDescending(x => x.FileCount),

                "Менше файлів" =>
                    data.OrderBy(x => x.FileCount)
            };

            string? pageSizeString = (PageSizeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (!int.TryParse(pageSizeString, out int pageSize))
            {
                pageSize = 20;
            }
            int totalItems = data.Count();

            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

            currentPage = Math.Max(1, Math.Min(currentPage, totalPages));

            data = data.Skip((currentPage - 1) * pageSize).Take(pageSize);

            HistoryGrid.ItemsSource = data.ToList();
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            LoadData();
        }
    }
}