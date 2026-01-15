using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EventManagement.Pages
{
    public partial class AuthPage : Page
    {
        private MainWindow _mainWindow;

        public AuthPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            EmailTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.LoginAsGuest();
        }

        private void AttemptLogin()
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email))
            {
                _mainWindow.ShowError("Введите электронную почту", "Ошибка ввода");
                EmailTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                _mainWindow.ShowError("Введите пароль", "Ошибка ввода");
                PasswordBox.Focus();
                return;
            }

            try
            {
                LoginButton.IsEnabled = false;
                GuestButton.IsEnabled = false;

                using (var context = new Entities())
                {
                    var user = context.Пользователи
                        .Include("Роли")
                        .FirstOrDefault(u => u.Почта == email && u.Пароль == password);

                    if (user != null)
                    {
                        _mainWindow.LoginUser(user);
                    }
                    else
                    {
                        _mainWindow.ShowError("Неверная электронная почта или пароль.", "Ошибка авторизации");
                        PasswordBox.Password = "";
                        PasswordBox.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка подключения к базе данных: {ex.Message}",
                                    "Ошибка соединения");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                GuestButton.IsEnabled = true;
            }
        }

        private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin();
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Опционально: валидация email
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Опционально: показ силы пароля
        }
    }
}