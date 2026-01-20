using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace EventManagement.Windows
{
    public partial class AddJuryToActivityWindow : Window
    {
        private MainWindow _mainWindow;
        private Активности _activity;
        private List<Пользователи> _availableJury;
        private List<ЖюриАктивности> _existingJury;

        public class JurySelectionItem
        {
            public int Id { get; set; }
            public string ФИО { get; set; }
            public bool IsSelected { get; set; }
        }

        public Активности Activity { get { return _activity; } }

        public AddJuryToActivityWindow(MainWindow mainWindow, Активности activity)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _activity = activity;
            DataContext = this;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем пользователей с ролью Жюри (Id = 3)
                    _availableJury = context.Пользователи
                        .Include(u => u.Роли)
                        .Where(u => u.Роли.Id == 3) // Роль "Жюри"
                        .OrderBy(u => u.ФИО)
                        .ToList();

                    // Загружаем уже назначенных членов жюри
                    _existingJury = context.ЖюриАктивности
                        .Where(j => j.АктивностьId == _activity.Id)
                        .ToList();

                    // Создаем список для выбора, исключая уже назначенных
                    var existingJuryIds = _existingJury.Select(j => j.ЖюриId).ToList();
                    var availableForSelection = _availableJury
                        .Where(j => !existingJuryIds.Contains(j.Id))
                        .ToList();

                    var jurySelectionList = availableForSelection.Select(j => new JurySelectionItem
                    {
                        Id = j.Id,
                        ФИО = j.ФИО,
                        IsSelected = false
                    }).ToList();

                    JuryItemsControl.ItemsSource = jurySelectionList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем выбранных членов жюри
                var selectedJury = new List<JurySelectionItem>();
                if (JuryItemsControl.ItemsSource is IEnumerable<JurySelectionItem> juryList)
                {
                    selectedJury = juryList.Where(j => j.IsSelected).ToList();
                }

                if (!selectedJury.Any())
                {
                    MessageBox.Show("Выберите хотя бы одного члена жюри", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new Entities())
                {
                    foreach (var juryMember in selectedJury)
                    {
                        var juryActivity = new ЖюриАктивности
                        {
                            АктивностьId = _activity.Id,
                            ЖюриId = juryMember.Id,
                            РольId = 1 // Базовая роль жюри
                        };
                        context.ЖюриАктивности.Add(juryActivity);
                    }

                    context.SaveChanges();

                    MessageBox.Show($"Добавлено {selectedJury.Count} член(ов) жюри",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

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