using EventManagement.Dialogs;
using EventManagement.Windows;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EventManagement.Pages
{
    public partial class EventDetailsPage : Page
    {
        private MainWindow _mainWindow;
        private Мероприятия _event;
        private List<Активности> _activities;

        public EventDetailsPage(MainWindow mainWindow, Мероприятия мероприятие)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _event = мероприятие;
            DataContext = _event;

            LoadEventDetails();
            LoadActivities();
            CheckUserPermissions();
            LoadEventImage();
        }

        private void LoadEventDetails()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем полную информацию о мероприятии
                    _event = context.Мероприятия
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .Include(e => e.Пользователи) // Организатор
                        .FirstOrDefault(e => e.Id == _event.Id);

                    DataContext = _event;
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadActivities()
        {
            try
            {
                using (var context = new Entities())
                {
                    _activities = context.Активности
                        .Include(a => a.Пользователи) // Модератор
                        .Where(a => a.МероприятиеId == _event.Id)
                        .OrderBy(a => a.День)
                        .ThenBy(a => a.ВремяНачала)
                        .ToList();

                    ActivitiesItemsControl.ItemsSource = _activities;

                    NoActivitiesText.Visibility = _activities.Any() ?
                        Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки активностей: {ex.Message}");
            }
        }

        private void CheckUserPermissions()
        {
            if (_mainWindow.CurrentUser == null) return;

            var userRole = _mainWindow.CurrentUser.Роли?.Название;

            // Кнопка редактирования мероприятия для организатора
            if (userRole == "Организатор")
            {
                EditEventButton.Visibility = Visibility.Visible;
                AddActivityButton.Visibility = Visibility.Visible;

                // Показываем кнопки удаления для активностей
                foreach (var item in ActivitiesItemsControl.Items)
                {
                    if (ActivitiesItemsControl.ItemContainerGenerator.ContainerFromItem(item) is ContentPresenter presenter)
                    {
                        if (VisualTreeHelper.GetChild(presenter, 0) is Border border)
                        {
                            var button = FindVisualChild<Button>(border, "DeleteActivityButton");
                            if (button != null)
                            {
                                button.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }

            // Кнопка участия для пользователя с ролью Участник
            if (userRole == "Участник")
            {
                // Проверяем, не участвует ли уже пользователь
                try
                {
                    using (var context = new Entities())
                    {
                        var isParticipant = context.УчастникиАктивностей
                            .Any(ua => ua.ПользовательId == _mainWindow.CurrentUser.Id &&
                                     ua.Активности.МероприятиеId == _event.Id);

                        if (!isParticipant)
                        {
                            JoinEventButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            JoinEventButton.Content = "Вы уже участвуете";
                            JoinEventButton.IsEnabled = false;
                            JoinEventButton.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибку
                }
            }
        }

        private void LoadEventImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(_event.Фото))
                {
                    string[] possiblePaths = {
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Events", _event.Фото),
                        System.IO.Path.Combine(System.IO.Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Events", _event.Фото),
                        System.IO.Path.Combine(Environment.CurrentDirectory, "Images", "Events", _event.Фото)
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            EventImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                            return;
                        }
                    }
                }

                // Загружаем изображение по умолчанию
                string[] defaultPaths = {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "default-event.png"),
                    System.IO.Path.Combine(System.IO.Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "default-event.png"),
                    System.IO.Path.Combine(Environment.CurrentDirectory, "Images", "default-event.png")
                };

                foreach (var path in defaultPaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        EventImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                        return;
                    }
                }
            }
            catch
            {
                // Игнорируем ошибку загрузки изображения
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    if (string.IsNullOrEmpty(childName) ||
                        (child is FrameworkElement frameworkElement && frameworkElement.Name == childName))
                    {
                        return typedChild;
                    }
                }

                var result = FindVisualChild<T>(child, childName);
                if (result != null) return result;
            }

            return null;
        }

        private void EditEventButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new AddEditEventWindow(_mainWindow, _event);
            editWindow.Owner = Window.GetWindow(this);

            if (editWindow.ShowDialog() == true)
            {
                // Обновляем данные
                LoadEventDetails();
                LoadEventImage();
            }
        }

        private void JoinEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow.CurrentUser == null) return;

            if (!_activities.Any())
            {
                _mainWindow.ShowError("В мероприятии нет активностей для участия");
                return;
            }

            var selectDialog = new SelectActivitiesDialog(_mainWindow.CurrentUser, _event);
            selectDialog.Owner = Window.GetWindow(this);

            if (selectDialog.ShowDialog() == true)
            {
                JoinEventButton.Content = "Вы уже участвуете";
                JoinEventButton.IsEnabled = false;
                _mainWindow.ShowMessage("Вы успешно зарегистрировались на мероприятие!");
            }
        }

        private void AddActivityButton_Click(object sender, RoutedEventArgs e)
        {
            var addActivityWindow = new AddActivityWindow(_mainWindow, _event);
            addActivityWindow.Owner = Window.GetWindow(this);

            if (addActivityWindow.ShowDialog() == true)
            {
                // Обновляем список активностей
                LoadActivities();
            }
        }

        private void DeleteActivityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int activityId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить активность?",
                                           "Подтверждение удаления",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            var activity = context.Активности.Find(activityId);
                            if (activity != null)
                            {
                                context.Активности.Remove(activity);
                                context.SaveChanges();

                                // Обновляем список
                                LoadActivities();
                                _mainWindow.ShowMessage("Активность успешно удалена");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _mainWindow.ShowError($"Ошибка при удалении: {ex.Message}");
                    }
                }
            }
        }
    }
}