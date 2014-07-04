using System;
using System.Runtime.InteropServices;

namespace Clipboard2Word
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardViewer(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ChangeClipboardChain(IntPtr hwnd, IntPtr hWndNext);
    }
}
