using System.Windows;
using System.Windows.Controls;

namespace EventManagement.Pages
{
    public partial class EventsPage : Page
    {
        private MainWindow _mainWindow;

        public EventsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // Показываем кнопку профиля только если пользователь авторизован
            ProfileButton.Visibility = _mainWindow.CurrentUser != null ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow.CurrentUser != null)
            {
                _mainWindow.MainFrame.Navigate(new ProfilePage(_mainWindow, _mainWindow.CurrentUser));
            }
        }
    }
}