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
    public SoundFileViewModel(string title,
                              Func<NightscoutIndicatorSettings, string> getValue,
                              Action<NightscoutIndicatorSettings, string> setValue)
        : base(title, getValue, setValue)
    {
    }

    /// <inheritdoc />
    protected override string FileFilter => "Sound files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*";

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
            MessageBox.Show($"Sound file not found: {FileName}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show($"Error playing sound {FileName}:{Environment.NewLine}{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Player_OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        Stop();
    }
}