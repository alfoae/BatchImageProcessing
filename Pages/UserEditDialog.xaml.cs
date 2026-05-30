using System.Windows;

namespace ImageProcessing.Pages
{
    public enum UserEditMode { Rename, Repassword }

    public partial class UserEditDialog : Window
    {
        public string ResultValue { get; private set; } = string.Empty;

        public UserEditDialog(string username, UserEditMode mode)
        {
            InitializeComponent();

            if (mode == UserEditMode.Repassword)
            {
                Title = "Змінити пароль";
                LabelText.Text = $"Новий пароль для \"{username}\":";
                InputTextBox.Visibility = Visibility.Collapsed;
                InputPasswordBox.Visibility = Visibility.Visible;
                InputPasswordBox.Focus();
            }
            else
            {
                Title = "Перейменувати користувача";
                LabelText.Text = $"Нове ім'я для \"{username}\":";
                InputTextBox.Text = username;
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string value = InputPasswordBox.Visibility == Visibility.Visible ? InputPasswordBox.Password : InputTextBox.Text;

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("Поле не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultValue = value;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
