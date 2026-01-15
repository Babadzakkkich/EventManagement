using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EventManagement.Dialogs
{
    public partial class SelectActivitiesDialog : Window
    {
        private Пользователи _user;
        private Мероприятия _event;
        private List<Активности> _activities;
        private List<int> _selectedActivityIds;

        public SelectActivitiesDialog(Пользователи user, Мероприятия мероприятие)
        {
            InitializeComponent();
            _user = user;
            _event = мероприятие;
            _selectedActivityIds = new List<int>();

            LoadActivities();
        }

        private void LoadActivities()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Загружаем активности мероприятия
                    _activities = context.Активности
                        .Where(a => a.МероприятиеId == _event.Id)
                        .OrderBy(a => a.День)
                        .ThenBy(a => a.ВремяНачала)
                        .ToList();

                    ActivitiesItemsControl.ItemsSource = _activities;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateSelection()
        {
            // Проверяем, выбрана ли хотя бы одна активность
            var selectedActivities = GetSelectedActivities();
            return selectedActivities.Any();
        }

        private List<Активности> GetSelectedActivities()
        {
            var selected = new List<Активности>();

            foreach (var activity in _activities)
            {
                // Находим CheckBox для этой активности
                var container = ActivitiesItemsControl.ItemContainerGenerator.ContainerFromItem(activity);
                if (container is ContentPresenter presenter)
                {
                    var checkBox = FindVisualChild<CheckBox>(presenter);
                    if (checkBox != null && checkBox.IsChecked == true)
                    {
                        selected.Add(activity);
                    }
                }
            }

            return selected;
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }

            return null;
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var activity in _activities)
            {
                var container = ActivitiesItemsControl.ItemContainerGenerator.ContainerFromItem(activity);
                if (container is ContentPresenter presenter)
                {
                    var checkBox = FindVisualChild<CheckBox>(presenter);
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var activity in _activities)
            {
                var container = ActivitiesItemsControl.ItemContainerGenerator.ContainerFromItem(activity);
                if (container is ContentPresenter presenter)
                {
                    var checkBox = FindVisualChild<CheckBox>(presenter);
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSelection())
            {
                MessageBox.Show("Выберите хотя бы одну активность для участия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedActivities = GetSelectedActivities();

                using (var context = new Entities())
                {
                    foreach (var activity in selectedActivities)
                    {
                        // Проверяем, не участвует ли уже пользователь в этой активности
                        var existingParticipation = context.УчастникиАктивностей
                            .FirstOrDefault(ua => ua.ПользовательId == _user.Id &&
                                                ua.АктивностьId == activity.Id);

                        if (existingParticipation == null)
                        {
                            var participation = new УчастникиАктивностей
                            {
                                ПользовательId = _user.Id,
                                АктивностьId = activity.Id
                            };

                            context.УчастникиАктивностей.Add(participation);
                        }
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