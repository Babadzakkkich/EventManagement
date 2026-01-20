using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace EventManagement.Windows
{
    public partial class AddEditActivityWindow : Window
    {
        private MainWindow _mainWindow;
        private Активности _currentActivity;
        private List<Пользователи> _moderators;
        private List<Пользователи> _juryUsers;
        private List<Мероприятия> _events;
        private List<ЖюриАктивности> _existingJury;
        private bool _isEditMode;
        private bool _canChangeEvent = true;

        // Класс для хранения состояния выбора жюри
        public class JurySelectionItem
        {
            public int Id { get; set; }
            public string ФИО { get; set; }
            public bool IsSelected { get; set; }
        }

        // Свойства для привязки данных
        public string WindowTitle => _isEditMode ? "Редактирование активности" : "Добавление активности";
        public string SaveButtonText => _isEditMode ? "Сохранить" : "Добавить";
        public Активности CurrentActivity => _currentActivity;
        public bool CanChangeEvent => _canChangeEvent;

        public AddEditActivityWindow(MainWindow mainWindow, Мероприятия мероприятие = null)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _isEditMode = false;
            _canChangeEvent = true;

            // Создание новой активности
            _currentActivity = new Активности
            {
                День = 1
            };

            // Если передано мероприятие, предварительно выбираем его
            if (мероприятие != null)
            {
                _currentActivity.МероприятиеId = мероприятие.Id;
                _canChangeEvent = false;
            }

            DataContext = this;
        }

        public AddEditActivityWindow(MainWindow mainWindow, Активности активность)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _isEditMode = true;
            _canChangeEvent = false; // В режиме редактирования нельзя менять мероприятие

            // Редактирование существующей активности
            _currentActivity = активность;

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем мероприятия для выбора
                    _events = context.Мероприятия
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .OrderBy(e => e.ДатаНачала)
                        .ToList();

                    EventComboBox.ItemsSource = _events;

                    // Загружаем модераторов (пользователи с ролью Модератор или Организатор)
                    _moderators = context.Пользователи
                        .Include(u => u.Роли)
                        .Where(u => u.Роли.Название == "Модератор" || u.Роли.Название == "Организатор")
                        .OrderBy(u => u.ФИО)
                        .ToList();

                    ModeratorComboBox.ItemsSource = _moderators;

                    // Загружаем жюри (пользователи с ролью Жюри, ID роли = 3)
                    _juryUsers = context.Пользователи
                        .Include(u => u.Роли)
                        .Where(u => u.Роли.Id == 3) // Роль с Id = 3 (Жюри)
                        .OrderBy(u => u.ФИО)
                        .ToList();

                    // Загружаем существующих членов жюри для активности (в режиме редактирования)
                    if (_isEditMode)
                    {
                        _existingJury = context.ЖюриАктивности
                            .Where(j => j.АктивностьId == _currentActivity.Id)
                            .ToList();
                    }
                    else
                    {
                        _existingJury = new List<ЖюриАктивности>();
                    }

                    // Создаем список для CheckBox'ов
                    var existingJuryIds = _existingJury.Select(j => j.ЖюриId).ToList();
                    var jurySelectionList = _juryUsers.Select(j => new JurySelectionItem
                    {
                        Id = j.Id,
                        ФИО = j.ФИО,
                        IsSelected = existingJuryIds.Contains(j.Id)
                    }).ToList();

                    JuryItemsControl.ItemsSource = jurySelectionList;

                    // Загружаем дни мероприятия
                    LoadDaysForEvent();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDaysForEvent()
        {
            try
            {
                if (_currentActivity.МероприятиеId == 0)
                {
                    DayComboBox.ItemsSource = null;
                    TimeComboBox.ItemsSource = null;
                    return;
                }

                using (var context = new Entities())
                {
                    var мероприятие = context.Мероприятия
                        .FirstOrDefault(e => e.Id == _currentActivity.МероприятиеId);

                    if (мероприятие == null)
                    {
                        DayComboBox.ItemsSource = null;
                        TimeComboBox.ItemsSource = null;
                        return;
                    }

                    // Заполняем дни мероприятия
                    var days = new List<int>();
                    for (int i = 1; i <= мероприятие.ДлительностьДней; i++)
                    {
                        days.Add(i);
                    }
                    DayComboBox.ItemsSource = days;

                    // Выбираем день активности или первый день
                    if (_currentActivity.День > 0 && days.Contains(_currentActivity.День))
                    {
                        DayComboBox.SelectedValue = _currentActivity.День;
                    }
                    else if (days.Count > 0)
                    {
                        DayComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дней мероприятия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateAvailableTimes(int day)
        {
            try
            {
                if (_currentActivity.МероприятиеId == 0)
                {
                    TimeComboBox.ItemsSource = null;
                    return;
                }

                using (var context = new Entities())
                {
                    // Получаем все активности в выбранный день (исключая текущую активность в режиме редактирования)
                    var dayActivities = context.Активности
                        .Where(a => a.МероприятиеId == _currentActivity.МероприятиеId && a.День == day)
                        .ToList();

                    if (_isEditMode)
                    {
                        dayActivities = dayActivities.Where(a => a.Id != _currentActivity.Id).ToList();
                    }

                    var orderedActivities = dayActivities.OrderBy(a => a.ВремяНачала).ToList();

                    // Начало мероприятия в 9:00
                    var startTime = new TimeSpan(9, 0, 0);

                    // Конец мероприятия в 18:00 (9 часов работы)
                    var endTime = new TimeSpan(18, 0, 0);

                    // Продолжительность активности (фиксированно 90 минут)
                    var activityDuration = new TimeSpan(1, 30, 0);

                    // Перерыв между активностями (15 минут)
                    var breakDuration = new TimeSpan(0, 15, 0);

                    var availableTimes = new List<TimeSpan>();
                    var currentTime = startTime;

                    // Пока есть время в расписании
                    while (currentTime + activityDuration <= endTime)
                    {
                        bool timeSlotAvailable = true;

                        // Проверяем, не пересекается ли время с существующими активностями
                        foreach (var activity in orderedActivities)
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
                            availableTimes.Add(currentTime);
                        }

                        // Переходим к следующему возможному времени (следующий час)
                        currentTime = currentTime.Add(new TimeSpan(1, 0, 0));
                    }

                    // Создаем список для отображения с форматированными строками
                    var displayTimes = availableTimes.Select(t => new
                    {
                        Time = t,
                        Display = $"{t:hh\\:mm}"
                    }).ToList();

                    TimeComboBox.ItemsSource = displayTimes;

                    // Выбираем время активности в режиме редактирования
                    if (_isEditMode && _currentActivity.ВремяНачала != default(TimeSpan))
                    {
                        var selectedTime = displayTimes.FirstOrDefault(t => t.Time == _currentActivity.ВремяНачала);
                        if (selectedTime != null)
                        {
                            TimeComboBox.SelectedValue = selectedTime.Time;
                        }
                        else
                        {
                            // Если текущее время недоступно, выбираем ближайшее доступное
                            var nearestTime = displayTimes.FirstOrDefault();
                            if (nearestTime != null)
                            {
                                TimeComboBox.SelectedValue = nearestTime.Time;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета времени: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EventComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadDaysForEvent();
        }

        private void DayComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DayComboBox.SelectedItem is int selectedDay && _currentActivity.МероприятиеId != 0)
            {
                CalculateAvailableTimes(selectedDay);
            }
        }

        private bool ValidateData()
        {
            if (_currentActivity.МероприятиеId == 0)
            {
                MessageBox.Show("Выберите мероприятие", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_currentActivity.Название))
            {
                MessageBox.Show("Введите название активности", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return false;
            }

            if (_currentActivity.День == 0)
            {
                MessageBox.Show("Выберите день мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DayComboBox.Focus();
                return false;
            }

            if (TimeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите время начала", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeComboBox.Focus();
                return false;
            }

            if (_currentActivity.МодераторId == 0)
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
                // Получаем выбранное время
                if (TimeComboBox.SelectedValue is TimeSpan selectedTime)
                {
                    _currentActivity.ВремяНачала = selectedTime;
                }
                else
                {
                    MessageBox.Show("Не удалось получить выбранное время", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем выбранных членов жюри
                var selectedJury = new List<JurySelectionItem>();
                if (JuryItemsControl.ItemsSource is IEnumerable<JurySelectionItem> juryList)
                {
                    selectedJury = juryList.Where(j => j.IsSelected).ToList();
                }

                using (var context = new Entities())
                {
                    if (_isEditMode)
                    {
                        // Обновляем существующую активность
                        var activityInDb = context.Активности
                            .Include(a => a.ЖюриАктивности)
                            .FirstOrDefault(a => a.Id == _currentActivity.Id);

                        if (activityInDb != null)
                        {
                            // Обновляем основные данные
                            activityInDb.Название = _currentActivity.Название;
                            activityInDb.День = _currentActivity.День;
                            activityInDb.ВремяНачала = _currentActivity.ВремяНачала;
                            activityInDb.МодераторId = _currentActivity.МодераторId;

                            // Обновляем жюри
                            var existingJuryIds = activityInDb.ЖюриАктивности.Select(j => j.ЖюриId).ToList();
                            var selectedJuryIds = selectedJury.Select(j => j.Id).ToList();

                            // Удаляем жюри, которые были сняты с выбора
                            var juryToRemove = activityInDb.ЖюриАктивности
                                .Where(j => !selectedJuryIds.Contains(j.ЖюриId))
                                .ToList();

                            foreach (var jury in juryToRemove)
                            {
                                context.ЖюриАктивности.Remove(jury);
                            }

                            // Добавляем новых членов жюри
                            var juryToAdd = selectedJuryIds
                                .Where(id => !existingJuryIds.Contains(id))
                                .Select(id => new ЖюриАктивности
                                {
                                    АктивностьId = activityInDb.Id,
                                    ЖюриId = id,
                                    РольId = 1
                                })
                                .ToList();

                            context.ЖюриАктивности.AddRange(juryToAdd);

                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        // Добавляем новую активность
                        context.Активности.Add(_currentActivity);
                        context.SaveChanges(); // Сохраняем, чтобы получить Id активности

                        // Добавляем выбранных членов жюри для активности
                        foreach (var juryMember in selectedJury)
                        {
                            var juryActivity = new ЖюриАктивности
                            {
                                АктивностьId = _currentActivity.Id,
                                ЖюриId = juryMember.Id,
                                РольId = 1
                            };
                            context.ЖюриАктивности.Add(juryActivity);
                        }

                        context.SaveChanges();
                    }

                    string juryInfo = selectedJury.Count > 0 ?
                        $" с {selectedJury.Count} член(ами) жюри" : " без жюри";

                    if (_isEditMode)
                    {
                        MessageBox.Show($"Активность '{_currentActivity.Название}' успешно обновлена{juryInfo}!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Активность '{_currentActivity.Название}' успешно добавлена{juryInfo}!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);

                var fullErrorMessage = string.Join("; ", errorMessages);
                MessageBox.Show($"Ошибка валидации: {fullErrorMessage}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpEx)
            {
                var innerException = dbUpEx.InnerException;
                if (innerException != null)
                {
                    MessageBox.Show($"Ошибка обновления БД: {innerException.Message}\n\nДетали: {innerException.InnerException?.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка обновления БД: {dbUpEx.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n\nТип: {ex.GetType().Name}", "Ошибка",
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