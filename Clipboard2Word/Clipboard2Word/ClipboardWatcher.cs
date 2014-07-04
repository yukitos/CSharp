using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Clipboard2Word
{
    public sealed class ClipboardWatcher : IDisposable
    {
        const int WM_DRAWCLIPBOARD = 0x0308;
        const int WM_CHANGECBCHAIN = 0x030D;

        IntPtr nextHandle;
        IntPtr handle;

        HwndSource hwndSource = null;

        public event EventHandler DrawClipboard;

        public ClipboardWatcher(IntPtr handle)
        {
            this.hwndSource = HwndSource.FromHwnd(handle);
            this.hwndSource.AddHook(this.WndProc);
            this.handle = handle;
            this.nextHandle = NativeMethods.SetClipboardViewer(this.handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DRAWCLIPBOARD)
            {
                NativeMethods.SendMessage(nextHandle, msg, wParam, lParam);
                this.raiseDrawClipboard();
                handled = true;
            }
            else if (msg == WM_CHANGECBCHAIN)
            {
                if (wParam == nextHandle)
                {
                    nextHandle = lParam;
                }
                else
                {
                    NativeMethods.SendMessage(nextHandle, msg, wParam, lParam);
                }
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void raiseDrawClipboard()
        {
            var handler = Interlocked.CompareExchange(ref DrawClipboard, null, null);
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            NativeMethods.ChangeClipboardChain(this.handle, nextHandle);
            this.hwndSource.Dispose();
        }
    }
}
