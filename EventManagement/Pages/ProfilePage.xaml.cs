using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EventManagement.Pages
{
    public partial class ProfilePage : Page
    {
        private MainWindow _mainWindow;
        private Пользователи _originalUser;
        private byte[] _newAvatarData;

        public ProfilePage(MainWindow mainWindow, Пользователи user)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _originalUser = user;
            DataContext = _mainWindow;
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                // Загрузка аватарки
                if (!string.IsNullOrEmpty(_mainWindow.CurrentUser.Фото))
                {
                    var path = GetAvatarPath(_mainWindow.CurrentUser.Фото);
                    if (!string.IsNullOrEmpty(path))
                    {
                        ProfileAvatarImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                            new Uri(path, UriKind.Absolute));
                    }
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private string GetAvatarPath(string fileName)
        {
            string[] possiblePaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Avatars", fileName),
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Avatars", fileName),
                Path.Combine(Environment.CurrentDirectory, "Images", "Avatars", fileName)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение для аватарки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Копируем файл в папку Avatars
                    var fileName = $"avatar_{_mainWindow.CurrentUser.Id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(openFileDialog.FileName)}";
                    var destinationPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Avatars", fileName);

                    // Создаем папку если её нет
                    var avatarsDir = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(avatarsDir))
                        Directory.CreateDirectory(avatarsDir);

                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    // Сохраняем только имя файла
                    _mainWindow.CurrentUser.Фото = fileName;

                    // Обновляем изображение
                    ProfileAvatarImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(destinationPath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowError($"Ошибка при загрузке изображения: {ex.Message}");
                }
            }
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == ShowOldPasswordCheckBox)
            {
                OldPasswordTextBox.Text = OldPasswordBox.Password;
                OldPasswordTextBox.Visibility = Visibility.Visible;
                OldPasswordBox.Visibility = Visibility.Collapsed;
            }
            else if (checkbox == ShowNewPasswordCheckBox)
            {
                NewPasswordTextBox.Text = NewPasswordBox.Password;
                NewPasswordTextBox.Visibility = Visibility.Visible;
                NewPasswordBox.Visibility = Visibility.Collapsed;
            }
            else if (checkbox == ShowConfirmPasswordCheckBox)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == ShowOldPasswordCheckBox)
            {
                OldPasswordBox.Password = OldPasswordTextBox.Text;
                OldPasswordBox.Visibility = Visibility.Visible;
                OldPasswordTextBox.Visibility = Visibility.Collapsed;
            }
            else if (checkbox == ShowNewPasswordCheckBox)
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
                NewPasswordBox.Visibility = Visibility.Visible;
                NewPasswordTextBox.Visibility = Visibility.Collapsed;
            }
            else if (checkbox == ShowConfirmPasswordCheckBox)
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация пароля
                string oldPassword = ShowOldPasswordCheckBox.IsChecked == true ?
                    OldPasswordTextBox.Text : OldPasswordBox.Password;

                string newPassword = ShowNewPasswordCheckBox.IsChecked == true ?
                    NewPasswordTextBox.Text : NewPasswordBox.Password;

                string confirmPassword = ShowConfirmPasswordCheckBox.IsChecked == true ?
                    ConfirmPasswordTextBox.Text : ConfirmPasswordBox.Password;

                // Проверяем, пытается ли пользователь изменить пароль
                bool changingPassword = !string.IsNullOrEmpty(oldPassword) ||
                                       !string.IsNullOrEmpty(newPassword) ||
                                       !string.IsNullOrEmpty(confirmPassword);

                if (changingPassword)
                {
                    // Проверяем старый пароль
                    if (_mainWindow.CurrentUser.Пароль != oldPassword)
                    {
                        _mainWindow.ShowError("Текущий пароль неверен");
                        return;
                    }

                    // Проверяем новый пароль
                    if (newPassword != confirmPassword)
                    {
                        _mainWindow.ShowError("Новые пароли не совпадают");
                        return;
                    }

                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        _mainWindow.CurrentUser.Пароль = newPassword;
                    }
                }

                // Сохраняем изменения в БД
                using (var context = new Entities())
                {
                    var userInDb = context.Пользователи.Find(_mainWindow.CurrentUser.Id);
                    if (userInDb != null)
                    {
                        userInDb.ФИО = _mainWindow.CurrentUser.ФИО;
                        userInDb.Почта = _mainWindow.CurrentUser.Почта;
                        userInDb.Телефон = _mainWindow.CurrentUser.Телефон;
                        userInDb.ДатаРождения = _mainWindow.CurrentUser.ДатаРождения;

                        if (!string.IsNullOrEmpty(_mainWindow.CurrentUser.Фото))
                            userInDb.Фото = _mainWindow.CurrentUser.Фото;

                        if (changingPassword && !string.IsNullOrEmpty(newPassword))
                            userInDb.Пароль = newPassword;

                        context.SaveChanges();

                        // Обновляем аватарку в главном окне
                        _mainWindow.UpdateAvatar();

                        _mainWindow.ShowMessage("Данные успешно сохранены");
                        _mainWindow.MainFrame.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.MainFrame.GoBack();
        }
    }
}