using EventManagement.Pages;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace EventManagement
{
    public partial class MainWindow : Window
    {
        public Пользователи CurrentUser { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            ShowAuthPage();
        }

        public void ShowAuthPage()
        {
            MainFrame.Navigate(new AuthPage(this));
            HideUserInfo();
            CurrentUser = null;
            UpdateBackButton();
            UpdateNavButtons();
        }

        public void LoginUser(Пользователи user)
        {
            CurrentUser = user;
            UpdateUserInfo();
            NavigateToMainPage();

            MessageBox.Show($"Добро пожаловать, {user.ФИО}!",
                          "Авторизация успешна",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        public void LoginAsGuest()
        {
            CurrentUser = null;
            HideUserInfo();
            NavigateToMainPage();

            MessageBox.Show("Вы вошли как гость. Доступен просмотр мероприятий.",
                          "Гостевой вход",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void UpdateUserInfo()
        {
            if (CurrentUser != null)
            {
                UserInfoPanel.Visibility = Visibility.Visible;
                UsernameTextBlock.Text = CurrentUser.ФИО;
                RoleTextBlock.Text = CurrentUser.Роли?.Название ?? "Пользователь";

                // Загрузка аватарки
                LoadAvatar();
            }
            else
            {
                HideUserInfo();
            }
        }

        private void LoadAvatar()
        {
            try
            {
                if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Фото))
                {
                    // Попробуем несколько путей
                    string[] possiblePaths = {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Avatars", CurrentUser.Фото),
                        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Avatars", CurrentUser.Фото),
                        Path.Combine(Environment.CurrentDirectory, "Images", "Avatars", CurrentUser.Фото)
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            AvatarImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                            return;
                        }
                    }
                }

                // Если аватарки нет, показываем заглушку
                AvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/default-avatar.png"));
            }
            catch (Exception)
            {
                // В случае ошибки просто оставляем пустое изображение
                AvatarImage.Source = null;
            }
        }

        private void HideUserInfo()
        {
            UserInfoPanel.Visibility = Visibility.Collapsed;
            UsernameTextBlock.Text = string.Empty;
            RoleTextBlock.Text = string.Empty;
            AvatarImage.Source = null;
        }

        private void NavigateToMainPage()
        {
            MainFrame.Navigate(new EventsPage(this));
            UpdateBackButton();
            UpdateNavButtons();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
                UpdateBackButton();
                UpdateNavButtons();
            }
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            UpdateBackButton();
            UpdateNavButtons();
        }

        private void UpdateBackButton()
        {
            BackButton.Visibility = MainFrame.CanGoBack &&
                                  !(MainFrame.Content is AuthPage)
                                  ? Visibility.Visible
                                  : Visibility.Collapsed;
        }

        private void UpdateNavButtons()
        {
            if (CurrentUser != null)
            {
                EventsNavButton.Visibility = Visibility.Visible;
                ActivitiesNavButton.Visibility = Visibility.Visible;
            }
            else
            {
                EventsNavButton.Visibility = Visibility.Visible;
                ActivitiesNavButton.Visibility = Visibility.Collapsed;
            }
        }

        private void EventsNavButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new EventsPage(this));
        }

        private void ActivitiesNavButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ActivitiesPage(this));
        }

        private void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser != null)
            {
                MainFrame.Navigate(new ProfilePage(this, CurrentUser));
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                                        "Выход",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentUser = null;
                HideUserInfo();
                ShowAuthPage();

                MessageBox.Show("Вы успешно вышли из системы.",
                              "Выход",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        public void ShowMessage(string message, string title = "Информация", MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public void ShowError(string message, string title = "Ошибка")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Метод для обновления аватарки после редактирования профиля
        public void UpdateAvatar()
        {
            LoadAvatar();
        }
    }
}