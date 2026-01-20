using EventManagement.Windows;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EventManagement.Pages
{
    public partial class ActivityDetailsPage : Page
    {
        private MainWindow _mainWindow;
        private Активности _activity;
        private List<ЖюриАктивности> _jury;
        private List<УчастникиАктивностей> _participants;

        public ActivityDetailsPage(MainWindow mainWindow, Активности активность)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _activity = активность;
            DataContext = _activity;

            LoadActivityDetails();
            LoadJury();
            LoadParticipants();
            CheckUserPermissions();
        }

        private void LoadActivityDetails()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем полную информацию об активности
                    _activity = context.Активности
                        .Include(a => a.Мероприятия)
                        .Include(a => a.Пользователи) // Модератор
                        .Include(a => a.ЖюриАктивности)
                        .Include(a => a.УчастникиАктивностей)
                        .FirstOrDefault(a => a.Id == _activity.Id);

                    DataContext = _activity;
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadJury()
        {
            try
            {
                using (var context = new Entities())
                {
                    _jury = context.ЖюриАктивности
                        .Include(j => j.Пользователи)
                        .Where(j => j.АктивностьId == _activity.Id)
                        .ToList();

                    JuryItemsControl.ItemsSource = _jury;

                    NoJuryText.Visibility = _jury.Any() ?
                        Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки жюри: {ex.Message}");
            }
        }

        private void LoadParticipants()
        {
            try
            {
                using (var context = new Entities())
                {
                    _participants = context.УчастникиАктивностей
                        .Include(p => p.Пользователи)
                        .Where(p => p.АктивностьId == _activity.Id)
                        .ToList();

                    ParticipantsItemsControl.ItemsSource = _participants;

                    NoParticipantsText.Visibility = _participants.Any() ?
                        Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки участников: {ex.Message}");
            }
        }

        private void CheckUserPermissions()
        {
            if (_mainWindow.CurrentUser == null) return;

            var userRole = _mainWindow.CurrentUser.Роли?.Название;

            // Проверяем, является ли пользователь организатором мероприятия
            bool isOrganizer = _activity.Мероприятия.ОрганизаторId == _mainWindow.CurrentUser.Id;

            if (userRole == "Организатор" && isOrganizer)
            {
                EditActivityButton.Visibility = Visibility.Visible;
                DeleteActivityButton.Visibility = Visibility.Visible;
                AddJuryButton.Visibility = Visibility.Visible;
            }
            else if (userRole == "Жюри")
            {
                // Проверяем, является ли пользователь жюри этой активности
                bool isJuryForThisActivity = _jury.Any(j => j.ЖюриId == _mainWindow.CurrentUser.Id);
                if (isJuryForThisActivity)
                {
                    // Показываем функционал для выставления оценок
                    // (это можно реализовать отдельно)
                }
            }
        }

        private void EditActivityButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new AddEditActivityWindow(_mainWindow, _activity);
            editWindow.Owner = Window.GetWindow(this);

            if (editWindow.ShowDialog() == true)
            {
                // Обновляем данные активности
                LoadActivityDetails();
                LoadJury();
                LoadParticipants();
            }
        }

        private void DeleteActivityButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем наличие жюри
            if (_jury.Any())
            {
                _mainWindow.ShowError("Нельзя удалить активность, так как для нее назначено жюри. " +
                                    "Сначала удалите всех членов жюри.");
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить активность \"{_activity.Название}\"?\n\n" +
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
                        var activityToDelete = context.Активности
                            .Include(a => a.ЖюриАктивности)
                            .Include(a => a.УчастникиАктивностей)
                            .FirstOrDefault(a => a.Id == _activity.Id);

                        if (activityToDelete == null)
                        {
                            _mainWindow.ShowError("Активность не найдена");
                            return;
                        }

                        // Проверяем, что нет жюри (дополнительная проверка)
                        if (activityToDelete.ЖюриАктивности.Any())
                        {
                            _mainWindow.ShowError("Нельзя удалить активность, так как для нее назначено жюри");
                            return;
                        }

                        // Удаляем активность
                        context.Активности.Remove(activityToDelete);
                        context.SaveChanges();

                        _mainWindow.ShowMessage($"Активность \"{_activity.Название}\" успешно удалена");

                        // Возвращаемся на страницу активностей
                        if (_mainWindow.MainFrame.CanGoBack)
                        {
                            _mainWindow.MainFrame.GoBack();
                        }
                        else
                        {
                            _mainWindow.MainFrame.Navigate(new ActivitiesPage(_mainWindow));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowError($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void AddJuryButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно для добавления жюри
            var addJuryWindow = new AddJuryToActivityWindow(_mainWindow, _activity);
            addJuryWindow.Owner = Window.GetWindow(this);

            if (addJuryWindow.ShowDialog() == true)
            {
                // Обновляем список жюри
                LoadJury();
            }
        }

        private void DeleteJuryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int juryId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить члена жюри из активности?",
                                           "Подтверждение удаления",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            var jury = context.ЖюриАктивности.Find(juryId);
                            if (jury != null)
                            {
                                context.ЖюриАктивности.Remove(jury);
                                context.SaveChanges();

                                // Обновляем список
                                LoadJury();
                                _mainWindow.ShowMessage("Член жюри успешно удален");
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