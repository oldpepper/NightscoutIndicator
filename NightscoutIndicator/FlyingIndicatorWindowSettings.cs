using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NightscoutIndicator;

public class FlyingIndicatorWindowSettings : INotifyPropertyChanged
{
    public const string SECTION_NAME = nameof(FlyingIndicatorWindowSettings);
    
    private decimal _left = 300;
    private decimal _top = 100;

    public decimal Left
    {
        get => _left;
        set
        {
            if (_left != value)
            {
                SetField(ref _left, value);
            }
        }
    }

    public decimal Top
    {
        get => _top;
        set
        {
            if (_top != value)
            {
                SetField(ref _top, value);
            }
        }
    }

#region PropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }

#endregion
}