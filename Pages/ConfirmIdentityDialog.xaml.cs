using ImageProcessing.Services;
using System.Windows;

namespace ImageProcessing.Pages
{
    public partial class ConfirmIdentityDialog : Window
    {
        public ConfirmIdentityDialog()
        {
            InitializeComponent();
            LoginBox.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var user = AuthService.Login(LoginBox.Text, PasswordBox.Password);

            if (user == null)
            {
                MessageBox.Show("Невірний логін або пароль.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordBox.Clear();
                return;
            }

            if (user.Login != AuthService.CurrentUser?.Login)
            {
                MessageBox.Show("Введені дані не збігаються з поточним акаунтом.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordBox.Clear();
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
