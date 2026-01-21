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
using System.Windows.Threading;

namespace EventManagement.Pages
{
    public partial class EventsPage : Page
    {
        private MainWindow _mainWindow;
        private List<Мероприятия> _allEvents;
        private List<Направления> _directions;
        private Dictionary<int, BitmapImage> _imageCache = new Dictionary<int, BitmapImage>();

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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Очищаем кеш при выходе со страницы
            ClearImageCache();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем мероприятия с связанными данными, включая победителя
                    _allEvents = context.Мероприятия
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .Include(e => e.Пользователи) // Организатор
                        .Include(e => e.Пользователи1) // Победитель
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

        private void ClearImageCache()
        {
            foreach (var image in _imageCache.Values)
            {
                image.StreamSource?.Dispose();
            }
            _imageCache.Clear();
        }

        private void UpdateEventsList(IEnumerable<Мероприятия> events)
        {
            ClearImageCache(); // Очищаем кеш перед обновлением
            EventsItemsControl.ItemsSource = events;

            if (!events.Any())
            {
                // Можно показать сообщение "Мероприятия не найдены"
            }

            // Используем более высокий приоритет для обновления
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EventsItemsControl.UpdateLayout();
                LoadEventImages();
            }), DispatcherPriority.Loaded);
        }

        private void LoadEventImages()
        {
            // Ждем обновления UI перед загрузкой изображений
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Загружаем изображения для всех карточек мероприятий
                    foreach (var item in EventsItemsControl.Items)
                    {
                        var container = EventsItemsControl.ItemContainerGenerator.ContainerFromItem(item);

                        // Если контейнер еще не создан, пропускаем
                        if (container == null)
                            continue;

                        // Ищем Image в контейнере
                        var image = FindVisualChild<Image>(container);
                        if (image != null && item is Мероприятия мероприятие)
                        {
                            LoadImageForEvent(image, мероприятие);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки изображений: {ex.Message}");
                }
            }), DispatcherPriority.Render);
        }

        private void LoadImageForEvent(Image imageControl, Мероприятия мероприятие)
        {
            try
            {
                // Проверяем кеш
                if (_imageCache.TryGetValue(мероприятие.Id, out var cachedImage))
                {
                    imageControl.Source = cachedImage;
                    return;
                }

                BitmapImage bitmap = null;

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
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Кешируем в памяти
                            bitmap.UriSource = new Uri(path, UriKind.Absolute);
                            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Игнорируем системный кеш
                            bitmap.EndInit();

                            // Проверяем, можно ли заморозить изображение
                            if (bitmap.CanFreeze)
                            {
                                bitmap.Freeze(); // Замораживаем для многопоточного доступа
                            }
                            break;
                        }
                    }
                }

                // Если не нашли, загружаем изображение по умолчанию
                if (bitmap == null)
                {
                    string[] defaultPaths = {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "default-event.png"),
                        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "default-event.png"),
                        Path.Combine(Environment.CurrentDirectory, "Images", "default-event.png")
                    };

                    foreach (var path in defaultPaths)
                    {
                        if (File.Exists(path))
                        {
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(path, UriKind.Absolute);
                            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                            bitmap.EndInit();

                            if (bitmap.CanFreeze)
                            {
                                bitmap.Freeze();
                            }
                            break;
                        }
                    }
                }

                // Сохраняем в кеш и устанавливаем изображение
                if (bitmap != null)
                {
                    _imageCache[мероприятие.Id] = bitmap;
                    imageControl.Source = bitmap;
                }
                else
                {
                    // Если изображение не найдено, устанавливаем null
                    imageControl.Source = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки изображения для мероприятия {мероприятие.Id}: {ex.Message}");
                imageControl.Source = null;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            // Пытаемся найти элемент по имени "EventImage"
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
                    // Проверяем имя элемента, если это Image
                    if (typeof(T) == typeof(Image))
                    {
                        if (child is FrameworkElement element && element.Name == "EventImage")
                            return result;
                    }
                    else
                    {
                        return result;
                    }
                }

                var childResult = FindVisualChild<T>(child);
                if (childResult != null) return childResult;
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

        // Обработчик для события SizeChanged - перезагружаем изображения при изменении размера
        private void EventsItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // При изменении размера перезагружаем изображения с новыми параметрами
            if (e.NewSize != e.PreviousSize)
            {
                LoadEventImages();
            }
        }
    }
}