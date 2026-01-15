using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace EventManagement.Windows
{
    public partial class AddActivityWindow : Window
    {
        private MainWindow _mainWindow;
        private Мероприятия _event;
        private List<Пользователи> _moderators;
        private List<TimeSpan> _availableTimes;

        public AddActivityWindow(MainWindow mainWindow, Мероприятия мероприятие)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _event = мероприятие;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем модераторов (пользователи с ролью Модератор или Организатор)
                    _moderators = context.Пользователи
                        .Where(u => u.Роли.Название == "Модератор" || u.Роли.Название == "Организатор")
                        .ToList();

                    ModeratorComboBox.ItemsSource = _moderators;

                    // Заполняем дни мероприятия
                    var days = new List<int>();
                    for (int i = 1; i <= _event.ДлительностьДней; i++)
                    {
                        days.Add(i);
                    }
                    DayComboBox.ItemsSource = days;

                    // Вычисляем доступное время для выбранного дня
                    CalculateAvailableTimes(1); // По умолчанию первый день
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateAvailableTimes(int day)
        {
            try
            {
                using (var context = new Entities())
                {
                    // Получаем все активности в выбранный день
                    var dayActivities = context.Активности
                        .Where(a => a.МероприятиеId == _event.Id && a.День == day)
                        .OrderBy(a => a.ВремяНачала)
                        .ToList();

                    // Начало мероприятия в 9:00
                    var startTime = new TimeSpan(9, 0, 0);

                    // Конец мероприятия в 18:00 (9 часов работы)
                    var endTime = new TimeSpan(18, 0, 0);

                    // Продолжительность активности (фиксированно 90 минут)
                    var activityDuration = new TimeSpan(1, 30, 0);

                    // Перерыв между активностями (15 минут)
                    var breakDuration = new TimeSpan(0, 15, 0);

                    _availableTimes = new List<TimeSpan>();
                    var currentTime = startTime;

                    // Пока есть время в расписании
                    while (currentTime + activityDuration <= endTime)
                    {
                        bool timeSlotAvailable = true;

                        // Проверяем, не пересекается ли время с существующими активностями
                        foreach (var activity in dayActivities)
                        {
                            var activityEndTime = activity.ВремяНачала + activityDuration;

                            // Проверяем пересечение временных интервалов с учетом перерыва
                            if ((currentTime >= activity.ВремяНачала - breakDuration &&
                                 currentTime < activityEndTime + breakDuration) ||
                                (currentTime + activityDuration > activity.ВремяНачала - breakDuration &&
                                 currentTime + activityDuration <= activityEndTime + breakDuration))
                            {
                                timeSlotAvailable = false;
                                break;
                            }
                        }

                        if (timeSlotAvailable)
                        {
                            _availableTimes.Add(currentTime);
                        }

                        // Переходим к следующему возможному времени (следующий час)
                        currentTime = currentTime.Add(new TimeSpan(1, 0, 0));
                    }

                    TimeComboBox.ItemsSource = _availableTimes;

                    // Форматируем время для отображения
                    TimeComboBox.DisplayMemberPath = "ToString";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета времени: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DayComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DayComboBox.SelectedItem is int selectedDay)
            {
                CalculateAvailableTimes(selectedDay);
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название активности", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return false;
            }

            if (DayComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите день мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DayComboBox.Focus();
                return false;
            }

            if (TimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите время начала", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeComboBox.Focus();
                return false;
            }

            if (ModeratorComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите модератора", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ModeratorComboBox.Focus();
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData()) return;

            try
            {
                var newActivity = new Активности
                {
                    Название = TitleTextBox.Text,
                    МероприятиеId = _event.Id,
                    День = (int)DayComboBox.SelectedItem,
                    ВремяНачала = (TimeSpan)TimeComboBox.SelectedItem,
                    МодераторId = ((Пользователи)ModeratorComboBox.SelectedItem).Id
                };

                using (var context = new Entities())
                {
                    context.Активности.Add(newActivity);
                    context.SaveChanges();

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}