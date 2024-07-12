using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PropertyChanged;

namespace NightscoutIndicator;

/// <summary>
/// File setting view model.
/// </summary>
[DoNotNotify]
public abstract partial class FileSettingViewModel : ObservableObject
{
    private readonly Func<NightscoutIndicatorSettings, string> _getValue;
    private readonly Action<NightscoutIndicatorSettings, string> _setValue;

    [ObservableProperty]
    private string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSettingViewModel"/> class.
    /// </summary>
    protected FileSettingViewModel(string title,
                                   Func<NightscoutIndicatorSettings, string> getValue,
                                   Action<NightscoutIndicatorSettings, string> setValue)
    {
        Title = title;
        _getValue = getValue;
        _setValue = setValue;
    }

    /// <summary>
    /// Setting title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Default file name.
    /// </summary>
    public string DefaultFileName { get; private set; }

    /// <summary>
    /// Full path of the selected file.
    /// </summary>
    public string FullPath => NightscoutIndicatorSettings.GetFullPath(FileName?.Trim());

    /// <summary>
    /// File filter of the open file dialog.
    /// </summary>
    protected abstract string FileFilter { get; }

    /// <summary>
    /// Loads the file name from the settings.
    /// </summary>
    public void Load(NightscoutIndicatorSettings settings, NightscoutIndicatorSettings defaults)
    {
        FileName = _getValue(settings);
        DefaultFileName = _getValue(defaults);
    }

    /// <summary>
    /// Stores the file name in the settings.
    /// </summary>
    public void Store(NightscoutIndicatorSettings settings)
    {
        _setValue(settings, FileName?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Handles the file name change.
    /// </summary>
    protected virtual void OnFileChanged()
    {
    }

    partial void OnFileNameChanged(string value)
    {
        OnFileChanged();
    }

    [RelayCommand]
    private void Browse()
    {
        var dialog = new OpenFileDialog
        {
            Title = $"Select file: {Title}",
            Filter = FileFilter,
            CheckFileExists = true
        };

        if (File.Exists(FullPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(FullPath);
            dialog.FileName = Path.GetFileName(FullPath);
        }

        if (dialog.ShowDialog() == true)
        {
            FileName = dialog.FileName;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        FileName = DefaultFileName;
    }
}