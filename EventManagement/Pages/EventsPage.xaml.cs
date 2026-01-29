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
        private DispatcherTimer _searchTimer;

        public EventsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            InitializeSearchTimer();

            LoadData();

            if (_mainWindow.CurrentUser != null && _mainWindow.CurrentUser.Роли?.Название == "Организатор")
            {
                OrganizerPanel.Visibility = Visibility.Visible;
            }
        }

        private void InitializeSearchTimer()
        {
            _searchTimer = new DispatcherTimer();
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEventImages();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ClearImageCache();

            if (_searchTimer != null)
            {
                _searchTimer.Stop();
                _searchTimer.Tick -= SearchTimer_Tick;
            }
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    _allEvents = context.Мероприятия
                        .Include(e => e.Города)
                        .Include(e => e.Направления)
                        .Include(e => e.Пользователи)
                        .Include(e => e.Пользователи1)
                        .ToList();

                    _directions = context.Направления.ToList();
                    DirectionComboBox.ItemsSource = _directions;
                    DirectionComboBox.DisplayMemberPath = "Название";
                    DirectionComboBox.SelectedIndex = -1;

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
            ClearImageCache();
            EventsItemsControl.ItemsSource = events;


            Dispatcher.BeginInvoke(new Action(() =>
            {
                EventsItemsControl.UpdateLayout();
                LoadEventImages();
            }), DispatcherPriority.Loaded);
        }

        private void LoadEventImages()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    foreach (var item in EventsItemsControl.Items)
                    {
                        var container = EventsItemsControl.ItemContainerGenerator.ContainerFromItem(item);

                        if (container == null)
                            continue;

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

                if (bitmap != null)
                {
                    _imageCache[мероприятие.Id] = bitmap;
                    imageControl.Source = bitmap;
                }
                else
                {
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

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
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

            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                filteredEvents = filteredEvents.Where(ev =>
                    ev.Название.ToLower().Contains(searchText) ||
                    (ev.Описание != null && ev.Описание.ToLower().Contains(searchText)) ||
                    ev.Города.Название.ToLower().Contains(searchText));
            }

            if (DirectionComboBox.SelectedItem is Направления selectedDirection)
            {
                filteredEvents = filteredEvents.Where(ev => ev.НаправлениеId == selectedDirection.Id);
            }

            if (DateFilterPicker.SelectedDate.HasValue)
            {
                var selectedDate = DateFilterPicker.SelectedDate.Value.Date;
                filteredEvents = filteredEvents.Where(ev => ev.ДатаНачала.Date == selectedDate);
            }

            UpdateEventsList(filteredEvents.ToList());
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();

            ApplyFilters();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
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
                LoadData();
            }
        }

        private void EventCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int eventId)
            {
                var мероприятие = _allEvents.FirstOrDefault(ev => ev.Id == eventId);
                if (мероприятие != null)
                {
                    _mainWindow.MainFrame.Navigate(new EventDetailsPage(_mainWindow, мероприятие));
                }
            }
        }

        private void EventsItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                LoadEventImages();
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _searchTimer.Stop();
                ApplyFilters();
            }
        }
    }
}