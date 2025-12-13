using NightscoutIndicator.Localization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace NightscoutIndicator;

/// <summary>
/// File setting view model.
/// </summary>
public abstract partial class FileSettingViewModel : ObservableObject
{
    private readonly Func<NightscoutIndicatorSettings, string> _getValue;
    private readonly Action<NightscoutIndicatorSettings, string> _setValue;
    private readonly string _titleKey;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSettingViewModel"/> class.
    /// </summary>
    protected FileSettingViewModel(string titleKey,
                                   Func<NightscoutIndicatorSettings, string> getValue,
                                   Action<NightscoutIndicatorSettings, string> setValue)
    {
        _titleKey = titleKey;
        _getValue = getValue;
        _setValue = setValue;

        // View models live as long as the application, so the subscription is never released
        LocalizationService.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Title));
    }

    /// <summary>
    /// Localized setting title.
    /// </summary>
    public string Title => LocalizationService.Instance[_titleKey];

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
        DefaultFileName = _getValue(defaults);
        FileName = _getValue(settings);
        ResetCommand.NotifyCanExecuteChanged();
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
            Title = LocalizationService.Instance.Format("Dialog.SelectFile", Title),
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

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        FileName = DefaultFileName;
    }

    private bool CanReset()
    {
        // Relative and absolute forms of the same file are treated as equal
        var defaultFullPath = NightscoutIndicatorSettings.GetFullPath(DefaultFileName?.Trim());

        try
        {
            return !string.Equals(Path.GetFullPath(FullPath), Path.GetFullPath(defaultFullPath), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // An invalid path typed by the user is never the default one
            return true;
        }
    }
}