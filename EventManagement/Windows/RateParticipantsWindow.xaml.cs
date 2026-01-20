using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace EventManagement.Windows
{
    public partial class RateParticipantsWindow : Window, INotifyPropertyChanged
    {
        private MainWindow _mainWindow;
        private Активности _activity;
        private ЖюриАктивности _juryMember;
        private List<ParticipantRatingItem> _participantRatings;
        private DateTime _lastSaveTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public Активности Activity => _activity;
        public string StatisticsText { get; private set; }
        public string LastSavedText { get; private set; }

        public class ParticipantRatingItem : INotifyPropertyChanged
        {
            public int ParticipantNumber { get; set; }
            public УчастникиАктивностей Participant { get; set; }

            private int _rating;
            public int Rating
            {
                get => _rating;
                set
                {
                    if (_rating != value)
                    {
                        _rating = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _comment;
            public string Comment
            {
                get => _comment;
                set
                {
                    if (_comment != value)
                    {
                        _comment = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _isSaved;
            public bool IsSaved
            {
                get => _isSaved;
                set
                {
                    if (_isSaved != value)
                    {
                        _isSaved = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public RateParticipantsWindow(MainWindow mainWindow, Активности activity)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _activity = activity;
            DataContext = this;

            LoadJuryMember();
            LoadParticipants();
            UpdateStatistics();
        }

        private void LoadJuryMember()
        {
            try
            {
                using (var context = new Entities())
                {
                    _juryMember = context.ЖюриАктивности
                        .Include(j => j.Пользователи)
                        .FirstOrDefault(j => j.АктивностьId == _activity.Id &&
                                            j.ЖюриId == _mainWindow.CurrentUser.Id);

                    if (_juryMember == null)
                    {
                        MessageBox.Show("Вы не являетесь жюри этой активности", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных жюри: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadParticipants()
        {
            try
            {
                using (var context = new Entities())
                {
                    var participants = context.УчастникиАктивностей
                        .Include(p => p.Пользователи)
                        .Include(p => p.Оценки)
                        .Where(p => p.АктивностьId == _activity.Id)
                        .ToList();

                    _participantRatings = new List<ParticipantRatingItem>();

                    int number = 1;
                    foreach (var participant in participants)
                    {
                        var existingRating = context.Оценки
                            .FirstOrDefault(o => o.ЖюриАктивностиId == _juryMember.Id &&
                                               o.УчастникАктивностиId == participant.Id);

                        var item = new ParticipantRatingItem
                        {
                            ParticipantNumber = number++,
                            Participant = participant,
                            Rating = existingRating?.Оценка ?? 0,
                            Comment = existingRating?.Комментарий ?? "",
                            IsSaved = existingRating != null
                        };

                        // Подписываемся на изменения
                        item.PropertyChanged += ParticipantRatingItem_PropertyChanged;
                        _participantRatings.Add(item);
                    }

                    ParticipantsItemsControl.ItemsSource = _participantRatings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParticipantRatingItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ParticipantRatingItem.Rating) ||
                e.PropertyName == nameof(ParticipantRatingItem.Comment))
            {
                var item = (ParticipantRatingItem)sender;
                item.IsSaved = false;
                UpdateStatistics();
            }
        }

        private void UpdateStatistics()
        {
            if (_participantRatings == null) return;

            int total = _participantRatings.Count;
            int rated = _participantRatings.Count(r => r.Rating > 0);
            int saved = _participantRatings.Count(r => r.IsSaved);
            int unsaved = _participantRatings.Count(r => !r.IsSaved);

            StatisticsText = $"Участников: {total} | Оценено: {rated}/{total} | Сохранено: {saved}/{total}";

            if (unsaved > 0)
            {
                StatisticsText += $" | Не сохранено: {unsaved}";
            }

            if (_lastSaveTime != default(DateTime))
            {
                LastSavedText = $"Последнее сохранение: {_lastSaveTime:HH:mm:ss}";
            }
            else
            {
                LastSavedText = "Изменения не сохранены";
            }

            OnPropertyChanged(nameof(StatisticsText));
            OnPropertyChanged(nameof(LastSavedText));
        }

        private bool SaveParticipantRating(ParticipantRatingItem item)
        {
            try
            {
                using (var context = new Entities())
                {
                    var existingRating = context.Оценки
                        .FirstOrDefault(o => o.ЖюриАктивностиId == _juryMember.Id &&
                                           o.УчастникАктивностиId == item.Participant.Id);

                    if (existingRating != null)
                    {
                        // Обновляем существующую оценку
                        existingRating.Оценка = item.Rating;
                        existingRating.Комментарий = item.Comment;
                        existingRating.ДатаОценки = DateTime.Now;
                    }
                    else
                    {
                        // Создаем новую оценку
                        var newRating = new Оценки
                        {
                            ЖюриАктивностиId = _juryMember.Id,
                            УчастникАктивностиId = item.Participant.Id,
                            Оценка = item.Rating,
                            Комментарий = item.Comment,
                            ДатаОценки = DateTime.Now
                        };
                        context.Оценки.Add(newRating);
                    }

                    context.SaveChanges();
                    item.IsSaved = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения оценки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            bool allSaved = true;

            foreach (var item in _participantRatings.Where(r => !r.IsSaved))
            {
                if (!SaveParticipantRating(item))
                {
                    allSaved = false;
                }
            }

            if (allSaved)
            {
                _lastSaveTime = DateTime.Now;
                UpdateStatistics();
                MessageBox.Show("Все оценки успешно сохранены", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var unsavedItems = _participantRatings.Count(r => !r.IsSaved);

            if (unsavedItems > 0)
            {
                var result = MessageBox.Show($"У вас есть {unsavedItems} несохраненных оценок. Закрыть без сохранения?",
                                           "Подтверждение",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}