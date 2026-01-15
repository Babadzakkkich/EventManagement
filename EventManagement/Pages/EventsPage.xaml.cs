using EventManagement.Windows;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EventManagement.Pages
{
    public partial class EventsPage : Page
    {
        private MainWindow _mainWindow;
        private List<Мероприятия> _allEvents;
        private List<Направления> _directions;

        public EventsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            LoadData();

            // Показываем панель организатора если пользователь авторизован и является организатором
            if (_mainWindow.CurrentUser != null && _mainWindow.CurrentUser.Роли?.Название == "Организатор")
            {
                OrganizerPanel.Visibility = Visibility.Visible;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем изображения после загрузки страницы
            LoadEventImages();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем мероприятия с связанными данными
                    _allEvents = context.Мероприятия
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .Include(e => e.Пользователи) // Организатор
                        .ToList();

                    // Загружаем направления для фильтра
                    _directions = context.Направления.ToList();
                    DirectionComboBox.ItemsSource = _directions;
                    DirectionComboBox.DisplayMemberPath = "Название";
                    DirectionComboBox.SelectedIndex = -1;

                    // Показываем все мероприятия
                    UpdateEventsList(_allEvents);
                }
            }
            catch (Exception ex)
            {
                _mainWindow.ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void UpdateEventsList(IEnumerable<Мероприятия> events)
        {
            EventsItemsControl.ItemsSource = events;

            if (!events.Any())
            {
                // Можно показать сообщение "Мероприятия не найдены"
            }
        }

        private void LoadEventImages()
        {
            // Загружаем изображения для всех карточек мероприятий
            foreach (var item in EventsItemsControl.Items)
            {
                if (EventsItemsControl.ItemContainerGenerator.ContainerFromItem(item) is ContentPresenter presenter)
                {
                    if (VisualTreeHelper.GetChild(presenter, 0) is Border border)
                    {
                        var image = FindVisualChild<Image>(border, "EventImage");
                        if (image != null && item is Мероприятия мероприятие)
                        {
                            LoadImageForEvent(image, мероприятие);
                        }
                    }
                }
            }
        }

        private void LoadImageForEvent(Image imageControl, Мероприятия мероприятие)
        {
            try
            {
                if (!string.IsNullOrEmpty(мероприятие.Фото))
                {
                    string[] possiblePaths = {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Events", мероприятие.Фото),
                        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Events", мероприятие.Фото),
                        Path.Combine(Environment.CurrentDirectory, "Images", "Events", мероприятие.Фото)
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            imageControl.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                            return;
                        }
                    }
                }

                // Загружаем изображение по умолчанию
                string[] defaultPaths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "default-event.png"),
                    Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "default-event.png"),
                    Path.Combine(Environment.CurrentDirectory, "Images", "default-event.png")
                };

                foreach (var path in defaultPaths)
                {
                    if (File.Exists(path))
                    {
                        imageControl.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                        return;
                    }
                }
            }
            catch
            {
                // Игнорируем ошибку загрузки изображения
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
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

        private void ApplyFilters()
        {
            var filteredEvents = _allEvents.AsQueryable();

            // Фильтр по поисковому запросу
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                filteredEvents = filteredEvents.Where(ev =>
                    ev.Название.ToLower().Contains(searchText) ||
                    (ev.Описание != null && ev.Описание.ToLower().Contains(searchText)) ||
                    ev.Города.Название.ToLower().Contains(searchText));
            }

            // Фильтр по направлению
            if (DirectionComboBox.SelectedItem is Направления selectedDirection)
            {
                filteredEvents = filteredEvents.Where(ev => ev.НаправлениеId == selectedDirection.Id);
            }

            // Фильтр по дате
            if (DateFilterPicker.SelectedDate.HasValue)
            {
                var selectedDate = DateFilterPicker.SelectedDate.Value.Date;
                filteredEvents = filteredEvents.Where(ev => ev.ДатаНачала.Date == selectedDate);
            }

            UpdateEventsList(filteredEvents.ToList());

            // Перезагружаем изображения для отфильтрованных мероприятий
            Dispatcher.BeginInvoke(new Action(() => LoadEventImages()));
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Можно добавить задержку для live search
            // ApplyFilters();
        }

        private void DirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilterPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            DirectionComboBox.SelectedIndex = -1;
            DateFilterPicker.SelectedDate = null;
            UpdateEventsList(_allEvents);
            Dispatcher.BeginInvoke(new Action(() => LoadEventImages()));
        }

        private void AddEventButton_Click(object sender, RoutedEventArgs e)
        {
            var addEventWindow = new AddEditEventWindow(_mainWindow);
            addEventWindow.Owner = Window.GetWindow(this);

            if (addEventWindow.ShowDialog() == true)
            {
                // Обновляем список мероприятий
                LoadData();
            }
        }

        private void EventCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int eventId)
            {
                // Используем другое имя переменной, чтобы избежать конфликта с параметром e
                var мероприятие = _allEvents.FirstOrDefault(ev => ev.Id == eventId);
                if (мероприятие != null)
                {
                    _mainWindow.MainFrame.Navigate(new EventDetailsPage(_mainWindow, мероприятие));
                }
            }
        }
    }
}