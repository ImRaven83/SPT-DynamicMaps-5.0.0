using System.Runtime.InteropServices;

namespace DynamicMaps.Utils
{
    // Local replacement for SPT.Custom.Utils.MessageBoxHelper, which no longer exists in the
    // SPTushonka 5.0.0 (IL2CPP) client modules. Shows a native Win32 message box directly so this
    // still works before/without the rest of the plugin (e.g. BepInEx logging) being set up.
    public static class MessageBoxHelper
    {
        public enum MessageBoxType : uint
        {
            OK = 0x0,
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);

        public static void Show(string message, string caption, MessageBoxType type)
        {
            MessageBoxW(0, message, caption, (uint)type);
        }
    }
}
