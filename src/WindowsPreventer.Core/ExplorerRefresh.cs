using System.Runtime.InteropServices;

namespace WindowsPreventer.Core;

/// <summary>
/// Notifies running Explorer processes that shell policy restrictions (e.g. NoViewContextMenu)
/// have changed, since Explorer caches these values and does not poll the registry.
/// </summary>
public static class ExplorerRefresh
{
    private const int HWND_BROADCAST = 0xffff;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        string lParam,
        int flags,
        int timeoutMilliseconds,
        out IntPtr result);

    /// <summary>
    /// Broadcasts a policy-changed notification so Explorer processes in the current session
    /// re-read shell restriction registry values. This does not guarantee an immediate effect
    /// for every restriction; restarting explorer.exe or signing out/in remains the reliable fallback.
    /// </summary>
    public static void NotifyPolicyChanged()
    {
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Policy", SMTO_ABORTIFHUNG, 5000, out _);
    }
}
