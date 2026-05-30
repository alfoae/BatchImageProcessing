using ImageProcessing.Services;
using System.Windows;
using System.Windows.Controls;

namespace ImageProcessing
{
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }

        private bool Validate()
        {
            LoginError.Visibility = string.IsNullOrWhiteSpace(LoginBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            PasswordError.Visibility = PasswordBox.SecurePassword.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

            return LoginError.Visibility == Visibility.Collapsed && PasswordError.Visibility == Visibility.Collapsed;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            var user = AuthService.Login(LoginBox.Text, PasswordBox.Password);

            if (user == null)
            {
                MessageBox.Show("Невірний логін або пароль");
                return;
            }

            AuthService.CurrentUser = user;

            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

            mainWindow._settingsPage.UpdatePermissions();

            mainWindow.MainFrame.Navigate(mainWindow._redactorPage);
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            bool success = AuthService.Register(LoginBox.Text, PasswordBox.Password);

            if (!success)
            {
                MessageBox.Show("Користувач уже існує");
                return;
            }

            MessageBox.Show("Реєстрація успішна");
        }
    }
}
