using System.Windows.Threading;

namespace NightscoutIndicator.Helpers;

public static class DispatcherExtensions
{
    public static void SafeInvoke(this DispatcherObject dispatcherObject, Action action)
    {
        if (dispatcherObject.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcherObject.Dispatcher.Invoke(action);
        }
    }

    public static void SafeBeginInvoke(this DispatcherObject dispatcherObject, Action action)
    {
        if (dispatcherObject.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcherObject.Dispatcher.BeginInvoke(action);
        }
    }
}