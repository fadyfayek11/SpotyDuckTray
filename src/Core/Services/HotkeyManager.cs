using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotyDuckTray;

public sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WmHotKey = 0x0312;
    private const int HotkeyId = 0x5344;

    private bool _registered;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered => _registered;

    public void Initialize()
    {
        if (Handle != IntPtr.Zero)
        {
            return;
        }

        CreateHandle(new CreateParams());
        AppLogger.Info("Hotkey manager handle created");
    }

    public bool Register(Keys modifiers, Keys key)
    {
        try
        {
            Initialize();
            Unregister();

            var modifierFlags = ToModifierFlags(modifiers);
            var keyCode = (uint)(key & Keys.KeyCode);

            _registered = RegisterHotKey(Handle, HotkeyId, modifierFlags, keyCode);

            if (_registered)
            {
                AppLogger.Info($"Hotkey registered: {modifiers} + {(key & Keys.KeyCode)}");
            }
            else
            {
                AppLogger.Warning($"Hotkey registration failed with Win32 error {Marshal.GetLastWin32Error()} for {modifiers} + {(key & Keys.KeyCode)}");
            }

            return _registered;
        }
        catch (Exception exception)
        {
            AppLogger.Error("Hotkey registration threw an exception", exception);
            _registered = false;
            return false;
        }
    }

    public void Unregister()
    {
        if (!_registered || Handle == IntPtr.Zero)
        {
            _registered = false;
            return;
        }

        try
        {
            if (!UnregisterHotKey(Handle, HotkeyId))
            {
                AppLogger.Warning($"Hotkey unregister failed with Win32 error {Marshal.GetLastWin32Error()}");
            }
            else
            {
                AppLogger.Info("Hotkey unregistered");
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Hotkey unregister threw an exception", exception);
        }
        finally
        {
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey && m.WParam == (IntPtr)HotkeyId)
        {
            try
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                AppLogger.Error("Hotkey event handler failed", exception);
            }
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Unregister();

        try
        {
            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
                AppLogger.Info("Hotkey manager handle destroyed");
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Destroying hotkey manager handle failed", exception);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
        AppLogger.Info("Hotkey manager disposed");
    }

    private static uint ToModifierFlags(Keys modifiers)
    {
        uint flags = 0;

        if (modifiers.HasFlag(Keys.Alt))
        {
            flags |= 0x0001;
        }

        if (modifiers.HasFlag(Keys.Control))
        {
            flags |= 0x0002;
        }

        if (modifiers.HasFlag(Keys.Shift))
        {
            flags |= 0x0004;
        }

        return flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}


