using System.Runtime.InteropServices;
using Finora.Application;
using Finora.Shared;

#if ANDROID
using Android.Views;
using Microsoft.Maui.ApplicationModel;
#endif

namespace Finora.App;

public sealed class SensitiveScreenService : ISensitiveScreenService
{
    public bool IsProtectionSupported
    {
        get
        {
#if ANDROID || WINDOWS
            return true;
#else
            return false;
#endif
        }
    }

    public Task<Result> SetProtectionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity?.Window is null) return Task.FromResult(Result.Failure("The Android window is unavailable."));
        if (enabled) activity.Window.AddFlags(WindowManagerFlags.Secure); else activity.Window.ClearFlags(WindowManagerFlags.Secure);
        return Task.FromResult(Result.Success());
#elif WINDOWS
        try
        {
            var windows = Microsoft.Maui.Controls.Application.Current?.Windows;
            var mauiWindow = windows is { Count: > 0 } ? windows[0] : null;
            if (mauiWindow?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window platformWindow)
                return Task.FromResult(Result.Failure("The Windows app window is unavailable."));
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
            var affinity = enabled ? WdaExcludeFromCapture : WdaNone;
            return Task.FromResult(SetWindowDisplayAffinity(hwnd, affinity)
                ? Result.Success()
                : Result.Failure("Windows did not enable capture protection for this window."));
        }
        catch (Exception)
        {
            return Task.FromResult(Result.Failure("Windows capture protection is unavailable in this app session."));
        }
#else
        return Task.FromResult(Result.Failure("This platform does not provide a supported API that can reliably block screenshots. Finora will avoid claiming otherwise."));
#endif
    }

#if WINDOWS
    private const uint WdaNone = 0x00000000;
    private const uint WdaExcludeFromCapture = 0x00000011;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);
#endif
}
