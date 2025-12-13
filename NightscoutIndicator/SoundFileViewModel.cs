using NightscoutIndicator.Localization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;

namespace NightscoutIndicator;

/// <summary>
/// Sound file setting view model.
/// </summary>
public partial class SoundFileViewModel : FileSettingViewModel
{
    private WaveOutEvent _player;
    private MediaFoundationReader _reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundFileViewModel"/> class.
    /// </summary>
    public SoundFileViewModel(string titleKey,
                              Func<NightscoutIndicatorSettings, string> getValue,
                              Action<NightscoutIndicatorSettings, string> setValue)
        : base(titleKey, getValue, setValue)
    {
    }

    /// <inheritdoc />
    protected override string FileFilter
    {
        get
        {
            var localization = LocalizationService.Instance;

            return $"{localization["Dialog.FilterSounds"]} (*.wav;*.mp3)|*.wav;*.mp3|{localization["Dialog.FilterAll"]} (*.*)|*.*";
        }
    }

    /// <summary>
    /// Stops the playback.
    /// </summary>
    public void Stop()
    {
        if (_player != null)
        {
            _player.PlaybackStopped -= Player_OnPlaybackStopped;
            _player.Dispose();
            _player = null;
        }

        if (_reader != null)
        {
            _reader.Dispose();
            _reader = null;
        }
    }

    [RelayCommand]
    private void Play()
    {
        Stop();

        if (!File.Exists(FullPath))
        {
            MessageBox.Show(LocalizationService.Instance.Format("Sound.NotFound", FileName), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _reader = new MediaFoundationReader(FullPath);
            _player = new WaveOutEvent();
            _player.PlaybackStopped += Player_OnPlaybackStopped;
            _player.Init(_reader);
            _player.Play();
        }
        catch (Exception ex)
        {
            Stop();
            MessageBox.Show(LocalizationService.Instance.Format("Alarm.PlayError", FileName, ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Player_OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        Stop();
    }
}