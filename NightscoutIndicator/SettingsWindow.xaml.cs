using System.Windows;

namespace NightscoutIndicator
{
    /// <summary>
    /// Settings window.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;
            ApiSecretBox.Password = viewModel.ApiSecret ?? string.Empty;

            _viewModel.CloseRequestedEvent += ViewModel_OnCloseRequestedEvent;
        }

        private void ViewModel_OnCloseRequestedEvent()
        {
            Close();
        }

        private void ApiSecretBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            // PasswordBox does not support binding, so the secret is synchronized manually
            _viewModel.ApiSecret = ApiSecretBox.Password;
        }

        private void ShowApiSecretButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            // The secret may have been edited in the visible text box
            ApiSecretBox.Password = _viewModel.ApiSecret ?? string.Empty;
        }

        /// <summary>
        /// Unsubscribes from the view model.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // The view model is a singleton and outlives the window
            _viewModel.CloseRequestedEvent -= ViewModel_OnCloseRequestedEvent;
            _viewModel.StopSounds();

            base.OnClosed(e);
        }
    }
}
