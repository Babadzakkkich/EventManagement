using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EventManagement.Windows
{
    public partial class AddEditEventWindow : Window
    {
        private MainWindow _mainWindow;
        private Мероприятия _currentEvent;
        private List<Города> _cities;
        private List<Направления> _directions;
        private bool _isEditMode;

        public string WindowTitle => _isEditMode ? "Редактирование мероприятия" : "Добавление мероприятия";
        public Мероприятия CurrentEvent => _currentEvent;

        public AddEditEventWindow(MainWindow mainWindow, Мероприятия мероприятие = null)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _isEditMode = мероприятие != null;

            if (_isEditMode)
            {
                // Редактирование существующего мероприятия
                _currentEvent = мероприятие;
            }
            else
            {
                // Создание нового мероприятия
                _currentEvent = new Мероприятия
                {
                    ДатаНачала = DateTime.Today,
                    ДлительностьДней = 1,
                    ОрганизаторId = _mainWindow.CurrentUser?.Id ?? 0
                };
            }

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            UpdatePreviewVisibility();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем города
                    _cities = context.Города.ToList();
                    CityComboBox.ItemsSource = _cities;

                    // Загружаем направления
                    _directions = context.Направления.ToList();
                    DirectionComboBox.ItemsSource = _directions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePreviewVisibility()
        {
            if (!string.IsNullOrEmpty(_currentEvent.Фото))
            {
                PreviewBorder.Visibility = Visibility.Visible;
                LoadPreviewImage();
            }
            else
            {
                PreviewBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadPreviewImage()
        {
            if (!string.IsNullOrEmpty(_currentEvent.Фото))
            {
                try
                {
                    string[] possiblePaths = {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Events", _currentEvent.Фото),
                        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Events", _currentEvent.Фото),
                        Path.Combine(Environment.CurrentDirectory, "Images", "Events", _currentEvent.Фото)
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            PreviewImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                            return;
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибку
                }
            }
        }

        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение для мероприятия"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Копируем файл в папку Events
                    var fileName = $"event_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(openFileDialog.FileName)}";
                    var destinationPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Events", fileName);

                    // Создаем папку если её нет
                    var eventsDir = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(eventsDir))
                        Directory.CreateDirectory(eventsDir);

                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    // Сохраняем только имя файла
                    _currentEvent.Фото = fileName;
                    ImagePathTextBox.Text = fileName;

                    // Обновляем превью
                    PreviewBorder.Visibility = Visibility.Visible;
                    PreviewImage.Source = new BitmapImage(new Uri(destinationPath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(_currentEvent.Название))
            {
                MessageBox.Show("Введите название мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return false;
            }

            if (_currentEvent.ДатаНачала < DateTime.Today)
            {
                MessageBox.Show("Дата начала не может быть в прошлом", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return false;
            }

            if (_currentEvent.ДлительностьДней < 1)
            {
                MessageBox.Show("Длительность должна быть не менее 1 дня", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return false;
            }

            if (_currentEvent.ГородId == 0)
            {
                MessageBox.Show("Выберите город", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CityComboBox.Focus();
                return false;
            }

            if (_currentEvent.НаправлениеId == 0)
            {
                MessageBox.Show("Выберите направление", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DirectionComboBox.Focus();
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData()) return;

            try
            {
                using (var context = new Entities())
                {
                    if (_isEditMode)
                    {
                        // Обновляем существующее мероприятие
                        var eventInDb = context.Мероприятия.Find(_currentEvent.Id);
                        if (eventInDb != null)
                        {
                            eventInDb.Название = _currentEvent.Название;
                            eventInDb.Описание = _currentEvent.Описание;
                            eventInDb.ДатаНачала = _currentEvent.ДатаНачала;
                            eventInDb.ДлительностьДней = _currentEvent.ДлительностьДней;
                            eventInDb.ГородId = _currentEvent.ГородId;
                            eventInDb.НаправлениеId = _currentEvent.НаправлениеId;

                            if (!string.IsNullOrEmpty(_currentEvent.Фото))
                                eventInDb.Фото = _currentEvent.Фото;
                        }
                    }
                    else
                    {
                        // Добавляем новое мероприятие
                        context.Мероприятия.Add(_currentEvent);
                    }

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