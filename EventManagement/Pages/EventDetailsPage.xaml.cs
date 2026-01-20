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
                        .Include(e => e.Пользователи1) // Победитель
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

            // Проверяем, является ли пользователь организатором этого мероприятия
            bool isOrganizer = _event.ОрганизаторId == _mainWindow.CurrentUser.Id;

            if (userRole == "Организатор" && isOrganizer)
            {
                EditEventButton.Visibility = Visibility.Visible;
                CalculateWinnerButton.Visibility = Visibility.Visible;
                DeleteEventButton.Visibility = Visibility.Visible;
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

        private void CalculateWinnerButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateWinner();
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
            var addActivityWindow = new AddEditActivityWindow(_mainWindow, _event);
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

        private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем наличие активностей
            if (_activities.Any())
            {
                _mainWindow.ShowError("Нельзя удалить мероприятие, так как для него существуют активности. " +
                                    "Сначала удалите все активности.");
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить мероприятие \"{_event.Название}\"?\n\n" +
                                       "Это действие нельзя отменить.",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new Entities())
                    {
                        // Загружаем мероприятие с зависимостями для проверки
                        var eventToDelete = context.Мероприятия
                            .Include(m => m.Активности)
                            .FirstOrDefault(m => m.Id == _event.Id);

                        if (eventToDelete == null)
                        {
                            _mainWindow.ShowError("Мероприятие не найдено");
                            return;
                        }

                        // Дополнительная проверка (на всякий случай)
                        if (eventToDelete.Активности.Any())
                        {
                            _mainWindow.ShowError("Нельзя удалить мероприятие, так как для него существуют активности");
                            return;
                        }

                        // Удаляем мероприятие
                        context.Мероприятия.Remove(eventToDelete);
                        context.SaveChanges();

                        _mainWindow.ShowMessage($"Мероприятие \"{_event.Название}\" успешно удалено");

                        // Возвращаемся на страницу мероприятий
                        if (_mainWindow.MainFrame.CanGoBack)
                        {
                            _mainWindow.MainFrame.GoBack();
                        }
                        else
                        {
                            _mainWindow.MainFrame.Navigate(new EventsPage(_mainWindow));
                        }
                    }
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
                {
                    // Проверяем наличие внешних ключей
                    var innerException = dbEx.InnerException?.InnerException;
                    if (innerException != null && innerException.Message.Contains("FOREIGN KEY constraint"))
                    {
                        _mainWindow.ShowError("Нельзя удалить мероприятие, так как на него ссылаются другие записи " +
                                            "(например, участники или жюри активностей).");
                    }
                    else
                    {
                        _mainWindow.ShowError($"Ошибка базы данных при удалении: {dbEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowError($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void CalculateWinner()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Проверяем, есть ли активности в мероприятии
                    if (!_activities.Any())
                    {
                        _mainWindow.ShowError("В мероприятии нет активностей для подсчета победителя");
                        return;
                    }

                    // Получаем всех участников мероприятия со всеми их оценками
                    var participantsWithScores = new Dictionary<int, ParticipantScore>();

                    foreach (var activity in _activities)
                    {
                        // Получаем участников этой активности
                        var activityParticipants = context.УчастникиАктивностей
                            .Include(up => up.Оценки)
                            .Where(up => up.АктивностьId == activity.Id)
                            .ToList();

                        foreach (var participant in activityParticipants)
                        {
                            if (!participantsWithScores.ContainsKey(participant.ПользовательId))
                            {
                                participantsWithScores[participant.ПользовательId] = new ParticipantScore
                                {
                                    UserId = participant.ПользовательId,
                                    UserName = participant.Пользователи?.ФИО ?? "Неизвестный участник"
                                };
                            }

                            // Суммируем все оценки участника в этой активности
                            var activityScore = participant.Оценки.Sum(o => o.Оценка);
                            participantsWithScores[participant.ПользовательId].TotalScore += activityScore;
                            participantsWithScores[participant.ПользовательId].ActivitiesCount++;
                            participantsWithScores[participant.ПользовательId].EvaluationsCount += participant.Оценки.Count;
                        }
                    }

                    if (!participantsWithScores.Any())
                    {
                        _mainWindow.ShowError("В мероприятии нет участников с оценками");
                        return;
                    }

                    // Вычисляем средний балл для каждого участника
                    foreach (var participant in participantsWithScores.Values)
                    {
                        if (participant.EvaluationsCount > 0)
                        {
                            participant.AverageScore = (double)participant.TotalScore / participant.EvaluationsCount;
                        }
                    }

                    // Сортируем участников по среднему баллу (по убыванию)
                    var sortedParticipants = participantsWithScores.Values
                        .OrderByDescending(p => p.AverageScore)
                        .ThenByDescending(p => p.TotalScore)
                        .ToList();

                    // Создаем окно для отображения результатов
                    var resultsWindow = new CalculateWinnerWindow(sortedParticipants, _event);
                    resultsWindow.Owner = Window.GetWindow(this);

                    if (resultsWindow.ShowDialog() == true)
                    {
                        // Если победитель выбран, обновляем данные
                        LoadEventDetails();
                    }
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка при подсчете победителя: {ex.Message}");
            }
        }

        // Вспомогательный класс для хранения результатов участников
        public class ParticipantScore
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public int TotalScore { get; set; }
            public int ActivitiesCount { get; set; }
            public int EvaluationsCount { get; set; }
            public double AverageScore { get; set; }
        }
    }
}