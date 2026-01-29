using EventManagement.Windows;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EventManagement.Pages
{
    public partial class ActivitiesPage : Page
    {
        private MainWindow _mainWindow;
        private List<Активности> _allActivities;
        private List<Мероприятия> _events;
        private bool _isOrganizer = false;

        public ActivitiesPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            LoadData();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CheckUserPermissions();
            LoadActivities();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    _allActivities = context.Активности
                        .Include(a => a.Мероприятия)
                        .Include(a => a.Пользователи)
                        .Include(a => a.ЖюриАктивности)
                        .Include(a => a.УчастникиАктивностей)
                        .ToList();

                    _events = context.Мероприятия
                        .OrderBy(e => e.Название)
                        .ToList();

                    var allEventsItem = new Мероприятия
                    {
                        Id = -1,
                        Название = "Все мероприятия"
                    };

                    _events.Insert(0, allEventsItem);
                    EventFilterComboBox.ItemsSource = _events;
                    EventFilterComboBox.DisplayMemberPath = "Название";
                    EventFilterComboBox.SelectedIndex = 0;

                    var days = new List<object> { "Все дни", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                    DayFilterComboBox.ItemsSource = days;
                    DayFilterComboBox.SelectedIndex = 0;

                    SortComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void CheckUserPermissions()
        {
            if (_mainWindow.CurrentUser != null)
            {
                var userRole = _mainWindow.CurrentUser.Роли?.Название;
                if (userRole == "Организатор")
                {
                    _isOrganizer = true;
                    OrganizerPanel.Visibility = Visibility.Visible;
                }
                else if (userRole == "Участник" || userRole == "Жюри")
                {
                    _isOrganizer = false;
                    OrganizerPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadActivities()
        {
            try
            {
                var activities = ApplyFiltersAndSorting(_allActivities);
                ActivitiesItemsControl.ItemsSource = activities;

                NoActivitiesText.Visibility = activities.Any() ?
                    Visibility.Collapsed : Visibility.Visible;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateActivityCardsStats();
                }), DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки активностей: {ex.Message}");
            }
        }

        private List<Активности> ApplyFiltersAndSorting(List<Активности> activities)
        {
            var result = activities.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                result = result.Where(a =>
                    a.Название.ToLower().Contains(searchText) ||
                    a.Мероприятия.Название.ToLower().Contains(searchText) ||
                    a.Пользователи.ФИО.ToLower().Contains(searchText) ||
                    a.Мероприятия.Города.Название.ToLower().Contains(searchText) ||
                    a.Мероприятия.Направления.Название.ToLower().Contains(searchText)
                );
            }

            if (EventFilterComboBox.SelectedItem is Мероприятия selectedEvent && selectedEvent.Id != -1)
            {
                result = result.Where(a => a.МероприятиеId == selectedEvent.Id);
            }

            if (DayFilterComboBox.SelectedItem != null && DayFilterComboBox.SelectedIndex > 0)
            {
                int selectedDay = (int)DayFilterComboBox.SelectedItem;
                result = result.Where(a => a.День == selectedDay);
            }

            if (SortComboBox.SelectedIndex == 1)
            {
                result = result.OrderBy(a => a.ВремяНачала);
            }
            else if (SortComboBox.SelectedIndex == 2)
            {
                result = result.OrderByDescending(a => a.ВремяНачала);
            }
            {
                result = result.OrderBy(a => a.Мероприятия.Название)
                               .ThenBy(a => a.День)
                               .ThenBy(a => a.ВремяНачала);
            }

            return result.ToList();
        }

        private void UpdateActivityCardsStats()
        {
            foreach (var item in ActivitiesItemsControl.Items)
            {
                var container = ActivitiesItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null && item is Активности activity)
                {
                    var juryTextBlock = FindVisualChild<TextBlock>(container, "JuryCountText");
                    var participantsTextBlock = FindVisualChild<TextBlock>(container, "ParticipantsCountText");

                    if (juryTextBlock != null)
                    {
                        juryTextBlock.Text = activity.ЖюриАктивности?.Count.ToString() ?? "0";
                    }

                    if (participantsTextBlock != null)
                    {
                        participantsTextBlock.Text = activity.УчастникиАктивностей?.Count.ToString() ?? "0";
                    }
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadActivities();
            }), DispatcherPriority.Background);
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadActivities();
        }

        private void EventFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadActivities();
        }

        private void DayFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadActivities();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            EventFilterComboBox.SelectedIndex = 0;
            DayFilterComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            LoadActivities();
        }

        private void AddActivityButton_Click(object sender, RoutedEventArgs e)
        {
            var addActivityWindow = new AddEditActivityWindow(_mainWindow);
            addActivityWindow.Owner = Window.GetWindow(this);

            if (addActivityWindow.ShowDialog() == true)
            {
                LoadData();
                LoadActivities();
            }
        }

        private void ActivityCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int activityId)
            {
                var активность = _allActivities.FirstOrDefault(a => a.Id == activityId);
                if (активность != null)
                {
                    if (_mainWindow.CurrentUser != null &&
                        _mainWindow.CurrentUser.Роли?.Название == "Жюри")
                    {
                        var isJury = активность.ЖюриАктивности
                            .Any(j => j.ЖюриId == _mainWindow.CurrentUser.Id);

                        if (isJury)
                        {
                            var result = MessageBox.Show("Вы являетесь жюри этой активности. Хотите перейти к оценке участников?",
                                                       "Быстрый доступ",
                                                       MessageBoxButton.YesNo,
                                                       MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                var rateWindow = new RateParticipantsWindow(_mainWindow, активность);
                                rateWindow.Owner = Window.GetWindow(this);
                                rateWindow.ShowDialog();
                                return;
                            }
                        }
                    }

                    _mainWindow.MainFrame.Navigate(new ActivityDetailsPage(_mainWindow, активность));
                }
            }
        }
    }
}