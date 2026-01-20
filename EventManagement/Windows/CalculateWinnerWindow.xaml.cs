using EventManagement.Pages;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace EventManagement.Windows
{
    public partial class CalculateWinnerWindow : Window
    {
        private List<EventDetailsPage.ParticipantScore> _participantScores;
        private Мероприятия _event;

        public Мероприятия Event => _event;

        public class RankedParticipantScore : EventDetailsPage.ParticipantScore
        {
            public int Rank { get; set; }
        }

        public CalculateWinnerWindow(List<EventDetailsPage.ParticipantScore> participantScores, Мероприятия мероприятие)
        {
            InitializeComponent();
            _participantScores = participantScores;
            _event = мероприятие;
            DataContext = this;

            LoadParticipants();
        }

        private void LoadParticipants()
        {
            try
            {
                // Добавляем ранги участникам
                var rankedParticipants = new List<RankedParticipantScore>();
                int rank = 1;

                foreach (var participant in _participantScores)
                {
                    var rankedParticipant = new RankedParticipantScore
                    {
                        UserId = participant.UserId,
                        UserName = participant.UserName,
                        TotalScore = participant.TotalScore,
                        ActivitiesCount = participant.ActivitiesCount,
                        EvaluationsCount = participant.EvaluationsCount,
                        AverageScore = participant.AverageScore,
                        Rank = rank++
                    };

                    rankedParticipants.Add(rankedParticipant);
                }

                ParticipantsItemsControl.ItemsSource = rankedParticipants;
                WinnerComboBox.ItemsSource = rankedParticipants;

                // По умолчанию выбираем первого участника (победителя)
                if (rankedParticipants.Count > 0)
                {
                    WinnerComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectWinnerButton_Click(object sender, RoutedEventArgs e)
        {
            if (WinnerComboBox.SelectedItem is RankedParticipantScore selectedWinner)
            {
                var result = MessageBox.Show($"Назначить победителем мероприятия участника:\n\n" +
                                           $"{selectedWinner.UserName}\n" +
                                           $"Средний балл: {selectedWinner.AverageScore:F2}\n" +
                                           $"Всего баллов: {selectedWinner.TotalScore}\n\n" +
                                           "Вы уверены?",
                                           "Подтверждение назначения победителя",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            var eventToUpdate = context.Мероприятия.Find(_event.Id);
                            if (eventToUpdate != null)
                            {
                                eventToUpdate.ПобедительId = selectedWinner.UserId;
                                context.SaveChanges();

                                MessageBox.Show($"Победитель мероприятия успешно назначен!\n\n" +
                                              $"Победитель: {selectedWinner.UserName}\n" +
                                              $"Средний балл: {selectedWinner.AverageScore:F2}",
                                              "Успех",
                                              MessageBoxButton.OK,
                                              MessageBoxImage.Information);

                                DialogResult = true;
                                Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении победителя: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите победителя из списка", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}